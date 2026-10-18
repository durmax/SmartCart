using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public enum ProductMatchType
{
    None,
    ReviewRequired,
    ExactEan,
    VerifiedAlias,
    NormalizedName
}

public sealed record ProductMatchResult(ProductMatchType Type, IReadOnlyList<Product> Candidates)
{
    public Product? ConfirmedProduct =>
        Type is ProductMatchType.ExactEan or ProductMatchType.VerifiedAlias or ProductMatchType.NormalizedName &&
        Candidates.Count == 1
            ? Candidates[0]
            : null;
}

public static partial class ProductMatcher
{
    private static readonly Regex PackagePattern = new(
        @"(?<amount>\d+(?:[.,]\d+)?)\s*(?<unit>kilograms?|kg|grams?|g|liters?|litres?|l|ml|milliliters?|millilitres?|pieces?|pcs?|pc|stk\.?|stücke?)\b?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ProductMatchResult Match(
        string name,
        IEnumerable<Product> products,
        string? ean = null,
        string? brand = null,
        string? retailer = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(products);

        var catalog = products.ToList();
        if (!string.IsNullOrWhiteSpace(ean))
        {
            var exactEanMatches = catalog
                .Where(product => !string.IsNullOrWhiteSpace(product.Ean) &&
                    NormalizeEan(product.Ean) == NormalizeEan(ean))
                .ToList();
            if (exactEanMatches.Count > 0)
            {
                return Resolve(exactEanMatches, ProductMatchType.ExactEan);
            }
        }

        var normalizedName = Normalize(name);
        var aliasMatches = catalog
            .Where(product => IsBrandCompatible(brand, product.Brand) &&
                HasCompatiblePackage(name, product.Name) &&
                product.Aliases.Any(alias => IsRetailerCompatible(retailer, alias.Retailer) &&
                    Normalize(alias.Alias) == normalizedName))
            .ToList();
        if (aliasMatches.Count > 0)
        {
            return Resolve(aliasMatches, ProductMatchType.VerifiedAlias);
        }

        var normalizedNameMatches = catalog
            .Where(product => IsBrandCompatible(brand, product.Brand) &&
                HasCompatiblePackage(name, product.Name) &&
                (Normalize(product.Name) == normalizedName ||
                 !string.IsNullOrWhiteSpace(product.NormalizedName) && Normalize(product.NormalizedName) == normalizedName))
            .ToList();
        if (normalizedNameMatches.Count > 0)
        {
            return Resolve(normalizedNameMatches, ProductMatchType.NormalizedName);
        }

        var candidates = catalog
            .Where(product => IsBrandCompatible(brand, product.Brand) &&
                HasCompatiblePackage(name, product.Name) &&
                AreConservativeNameCandidates(name, product.Name, brand, product.Brand))
            .ToList();

        return candidates.Count == 0
            ? new ProductMatchResult(ProductMatchType.None, [])
            : new ProductMatchResult(ProductMatchType.ReviewRequired, candidates);
    }

    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var packageMatch = PackagePattern.Match(value);
        var package = packageMatch.Success ? NormalizePackage(packageMatch) : null;
        var name = packageMatch.Success
            ? value.Remove(packageMatch.Index, packageMatch.Length)
            : value;
        name = Regex.Replace(name, @"(?<=\d),(?=\d)", ".");
        name = name.Normalize(NormalizationForm.FormD);

        var normalized = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var current = char.ToLowerInvariant(character);
            if (current == 'ß')
            {
                normalized.Append("ss");
            }
            else
            {
                normalized.Append(char.IsLetterOrDigit(current) ? current : ' ');
            }
        }

        var result = Regex.Replace(normalized.ToString(), @"\s+", " ").Trim();
        return package is null ? result : $"{result} {package}".Trim();
    }

    private static ProductMatchResult Resolve(List<Product> matches, ProductMatchType type)
    {
        var uniqueMatches = matches.DistinctBy(product => product.Id).ToList();
        return uniqueMatches.Count == 1
            ? new ProductMatchResult(type, uniqueMatches)
            : new ProductMatchResult(ProductMatchType.ReviewRequired, uniqueMatches);
    }

    private static bool IsRetailerCompatible(string? requestedRetailer, string? aliasRetailer) =>
        string.IsNullOrWhiteSpace(aliasRetailer) ||
        !string.IsNullOrWhiteSpace(requestedRetailer) &&
        string.Equals(requestedRetailer.Trim(), aliasRetailer.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool IsBrandCompatible(string? requestedBrand, string? productBrand) =>
        string.IsNullOrWhiteSpace(requestedBrand) ||
        string.IsNullOrWhiteSpace(productBrand) ||
        Normalize(requestedBrand) == Normalize(productBrand);

    private static bool HasCompatiblePackage(string candidateName, string productName)
    {
        var candidatePackage = GetPackage(candidateName);
        var productPackage = GetPackage(productName);
        return candidatePackage is null || productPackage is null || candidatePackage == productPackage;
    }

    private static bool AreConservativeNameCandidates(
        string candidateName,
        string productName,
        string? candidateBrand,
        string? productBrand)
    {
        var candidateTokens = GetCoreTokens(candidateName, candidateBrand);
        var productTokens = GetCoreTokens(productName, productBrand);
        var sharedTokens = candidateTokens.Intersect(productTokens).Count();
        if (sharedTokens < 2)
        {
            return false;
        }

        return sharedTokens / (double)Math.Min(candidateTokens.Count, productTokens.Count) >= 0.8;
    }

    private static HashSet<string> GetCoreTokens(string value, string? brand)
    {
        var name = PackagePattern.Replace(value, " ");
        var tokens = Normalize(name).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(brand))
        {
            foreach (var token in Normalize(brand).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                tokens.Remove(token);
            }
        }

        return tokens;
    }

    private static string? GetPackage(string value)
    {
        var match = PackagePattern.Match(value);
        return match.Success ? NormalizePackage(match) : null;
    }

    private static string NormalizePackage(Match match)
    {
        var amount = decimal.Parse(
            match.Groups["amount"].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value.ToLowerInvariant().TrimEnd('.');

        if (unit is "kg" or "kilogram" or "kilograms")
        {
            amount *= 1000;
            unit = "g";
        }
        else if (unit is "gram" or "grams")
        {
            unit = "g";
        }
        else if (unit is "l" or "liter" or "liters" or "litre" or "litres")
        {
            amount *= 1000;
            unit = "ml";
        }
        else if (unit is "milliliter" or "milliliters" or "millilitre" or "millilitres")
        {
            unit = "ml";
        }
        else if (unit is "piece" or "pieces" or "pc" or "pcs" or "stk" or "stück" or "stücke")
        {
            unit = "pc";
        }

        return $"{amount.ToString("0.####", CultureInfo.InvariantCulture)}{unit}";
    }

    private static string NormalizeEan(string ean) =>
        string.Concat(ean.Where(char.IsDigit));
}
