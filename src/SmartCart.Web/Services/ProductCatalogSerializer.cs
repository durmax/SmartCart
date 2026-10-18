using System.Text.Json;
using System.Text.Json.Serialization;
using SmartCart.Application.Services;
using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public sealed class ProductCatalogDocument
{
    public int SchemaVersion { get; init; } = ProductCatalogSerializer.CurrentSchemaVersion;
    public List<PersonalProduct>? Products { get; init; } = [];
}

public static class ProductCatalogSerializer
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Export(IEnumerable<PersonalProduct> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        var normalizedProducts = ValidateAndNormalize(products.ToList());
        return JsonSerializer.Serialize(new ProductCatalogDocument { Products = normalizedProducts }, JsonOptions);
    }

    public static IReadOnlyList<PersonalProduct> Import(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        ProductCatalogDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ProductCatalogDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The product file is not valid JSON.", exception);
        }

        if (document is null || document.Products is null)
        {
            throw new InvalidDataException("The product file is missing its product list.");
        }

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Product file version {document.SchemaVersion} is not supported.");
        }

        return ValidateAndNormalize(document.Products);
    }

    public static PersonalProduct ValidateAndNormalizeProduct(PersonalProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return ValidateAndNormalize([product])[0];
    }

    private static List<PersonalProduct> ValidateAndNormalize(List<PersonalProduct> products)
    {
        var ids = new HashSet<Guid>();
        var aliasIds = new HashSet<Guid>();
        var productKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var product in products)
        {
            if (product is null || product.Id == Guid.Empty || !ids.Add(product.Id))
            {
                throw new InvalidDataException("Each product must have a unique, non-empty id.");
            }

            product.Name = product.Name?.Trim() ?? string.Empty;
            if (product.Name.Length == 0)
            {
                throw new InvalidDataException("Each product must have a name.");
            }

            product.Brand = NormalizeOptional(product.Brand);
            product.NormalizedName = ProductMatcher.Normalize(product.Name);
            product.Ean = NormalizeEan(product.Ean);
            product.PackageUnit = NormalizeOptional(product.PackageUnit)?.ToLowerInvariant();

            if (product.UsualQuantity is <= 0 || product.PackageQuantity is <= 0 ||
                (product.PackageQuantity.HasValue != !string.IsNullOrWhiteSpace(product.PackageUnit)))
            {
                throw new InvalidDataException("Product quantities and package units must be positive and consistent.");
            }

            if (product.Ean is not null &&
                (product.Ean.Length is not (8 or 12 or 13 or 14) || !product.Ean.All(char.IsDigit)))
            {
                throw new InvalidDataException("EAN values must contain 8, 12, 13, or 14 digits.");
            }

            var productKey = $"{product.NormalizedName}|{ProductMatcher.Normalize(product.Brand ?? string.Empty)}|{product.PackageQuantity}|{product.PackageUnit}";
            if (!productKeys.Add(productKey))
            {
                throw new InvalidDataException("The product list contains duplicate products.");
            }

            product.Aliases ??= [];
            var aliasKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var alias in product.Aliases)
            {
                if (alias is null)
                {
                    throw new InvalidDataException("The product list contains an invalid alias.");
                }

                alias.Alias = alias.Alias?.Trim() ?? string.Empty;
                alias.Retailer = NormalizeOptional(alias.Retailer);
                if (alias.Alias.Length == 0 || alias.Id == Guid.Empty || !aliasIds.Add(alias.Id) ||
                    !aliasKeys.Add($"{ProductMatcher.Normalize(alias.Alias)}|{alias.Retailer}"))
                {
                    throw new InvalidDataException("Each alias must have a unique id and a non-empty value.");
                }
            }
        }

        return products;
    }

    private static string? NormalizeEan(string? ean)
    {
        if (string.IsNullOrWhiteSpace(ean))
        {
            return null;
        }

        var normalized = ean.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (normalized.Any(character => character is < '0' or > '9'))
        {
            throw new InvalidDataException("EAN values may contain only digits, spaces, and hyphens.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
