using System.Globalization;
using System.Text.RegularExpressions;
using SmartCart.Application.Services;
using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public sealed class OfferComparisonService
{
    private static readonly Regex PackageSuffix = new(
        @"\s+\d+(?:\.\d+)?(?:kg|g|l|ml|pc)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public OfferComparisonResult Compare(
        PersonalProduct product,
        IEnumerable<ComparableRetailerOffer> offers,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(offers);

        var comparisonTime = now ?? DateTimeOffset.UtcNow;
        var results = offers.Select(offer => CompareOne(product, offer, comparisonTime)).ToList();
        return new OfferComparisonResult(results, SelectBestKnownOffer(product, results));
    }

    private static OfferComparisonItem CompareOne(PersonalProduct product, ComparableRetailerOffer offer, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (offer.IsDevelopmentDemo)
        {
            return Result(offer, OfferComparisonStatus.DevelopmentDemo, OfferMatchConfidence.NoMatch,
                "Development sample data is not a real retailer offer.");
        }

        if (!offer.IsSourceAuthorized || !Uri.TryCreate(offer.SourceUrl, UriKind.Absolute, out var sourceUri) ||
            sourceUri.Scheme != Uri.UriSchemeHttps)
        {
            return Result(offer, OfferComparisonStatus.UnauthorizedSource, OfferMatchConfidence.NoMatch,
                "No authorized HTTPS source is verified for this offer.");
        }

        if (offer.IsRestricted)
        {
            return Result(offer, OfferComparisonStatus.Restricted, OfferMatchConfidence.NoMatch,
                offer.RestrictionDetails ?? "This offer has restrictions and is excluded from comparison.");
        }

        if (offer.OfferPrice is null or <= 0)
        {
            return Result(offer, OfferComparisonStatus.InvalidPrice, OfferMatchConfidence.NoMatch,
                "A positive offer price is required.");
        }

        var validity = GetValidityStatus(offer, now);
        if (validity.Status is not (OfferComparisonStatus.Current or OfferComparisonStatus.Upcoming))
        {
            return Result(offer, validity.Status, OfferMatchConfidence.NoMatch, validity.Reason);
        }

        var match = MatchProduct(product, offer);
        if (match.Confidence == OfferMatchConfidence.NoMatch)
        {
            return Result(offer, OfferComparisonStatus.NoProductMatch, match.Confidence,
                "The offer could not be identified as the saved product.");
        }

        if (match.Confidence == OfferMatchConfidence.NeedsReview)
        {
            return Result(offer, OfferComparisonStatus.ProductMatchNeedsReview, match.Confidence,
                "Product identity or package details are incomplete or conflicting; review is required.");
        }

        var pricePerUnit = TryGetPricePerBaseUnit(offer, out var baseUnit);
        return new OfferComparisonItem(offer, validity.Status, match.Confidence, pricePerUnit, baseUnit,
            validity.Reason);
    }

    private static (OfferComparisonStatus Status, string Reason) GetValidityStatus(
        ComparableRetailerOffer offer,
        DateTimeOffset now)
    {
        if (offer.ValidFrom is null || offer.ValidTo is null)
        {
            return (OfferComparisonStatus.UnknownValidity, "A complete published validity period is required.");
        }

        if (offer.ValidTo < offer.ValidFrom)
        {
            return (OfferComparisonStatus.InvalidValidity, "The offer validity dates are inconsistent.");
        }

        if (offer.ValidTo < now)
        {
            return (OfferComparisonStatus.Expired, "The offer has expired.");
        }

        return offer.ValidFrom > now
            ? (OfferComparisonStatus.Upcoming, "Officially published upcoming offer.")
            : (OfferComparisonStatus.Current, "Current offer from a verified source.");
    }

    private static (OfferMatchConfidence Confidence, string? BaseDimension) MatchProduct(
        PersonalProduct product,
        ComparableRetailerOffer offer)
    {
        var productHasEan = !string.IsNullOrWhiteSpace(product.Ean);
        var offerHasEan = !string.IsNullOrWhiteSpace(offer.Ean);
        var productEan = NormalizeEan(product.Ean);
        var offerEan = NormalizeEan(offer.Ean);
        if (productHasEan && (productEan is null || !IsValidEan(productEan)) ||
            offerHasEan && (offerEan is null || !IsValidEan(offerEan)))
        {
            return (OfferMatchConfidence.NeedsReview, null);
        }

        if (productEan is not null && offerEan is not null)
        {
            if (!string.Equals(productEan, offerEan, StringComparison.Ordinal))
            {
                return (OfferMatchConfidence.NoMatch, null);
            }

            if (HasBrandConflict(product.Brand, offer.Brand) || HasPackageConflict(product, offer))
            {
                return (OfferMatchConfidence.NeedsReview, null);
            }

            return (OfferMatchConfidence.ExactEan, GetPackageDimension(offer.PackageQuantity, offer.PackageUnit));
        }

        if (HasBrandConflict(product.Brand, offer.Brand))
        {
            return (OfferMatchConfidence.NoMatch, null);
        }

        var offerName = NormalizeCoreName(offer.ProductName);
        var nameMatches = NormalizeCoreName(product.Name) == offerName;
        if (!nameMatches)
        {
            nameMatches = product.Aliases.Any(alias =>
                (string.IsNullOrWhiteSpace(alias.Retailer) ||
                 string.Equals(alias.Retailer, offer.Retailer, StringComparison.OrdinalIgnoreCase)) &&
                NormalizeCoreName(alias.Alias) == offerName);
        }

        if (!nameMatches)
        {
            var match = ProductMatcher.Match(offer.ProductName, [product.ToDomainProduct()], offer.Ean,
                offer.Brand, offer.Retailer);
            return match.Type == ProductMatchType.ReviewRequired
                ? (OfferMatchConfidence.NeedsReview, null)
                : (OfferMatchConfidence.NoMatch, null);
        }

        if (string.IsNullOrWhiteSpace(product.Brand) || string.IsNullOrWhiteSpace(offer.Brand) ||
            !string.Equals(ProductMatcher.Normalize(product.Brand), ProductMatcher.Normalize(offer.Brand), StringComparison.Ordinal))
        {
            return (OfferMatchConfidence.NeedsReview, null);
        }

        var productDimension = GetPackageDimension(product.PackageQuantity, product.PackageUnit);
        var offerDimension = GetPackageDimension(offer.PackageQuantity, offer.PackageUnit);
        if (productDimension is null || offerDimension is null)
        {
            return (OfferMatchConfidence.NeedsReview, null);
        }

        return string.Equals(productDimension, offerDimension, StringComparison.Ordinal)
            ? (OfferMatchConfidence.Confirmed, offerDimension)
            : (OfferMatchConfidence.NoMatch, null);
    }

    private static OfferComparisonItem? SelectBestKnownOffer(
        PersonalProduct product,
        IReadOnlyList<OfferComparisonItem> results)
    {
        var current = results.Where(result => result.Status == OfferComparisonStatus.Current).ToList();
        var candidates = current.Count > 0
            ? current
            : results.Where(result => result.Status == OfferComparisonStatus.Upcoming).ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.All(candidate => candidate.PricePerBaseUnit.HasValue) &&
            candidates.Select(candidate => candidate.BaseUnit).Distinct(StringComparer.Ordinal).Count() == 1)
        {
            return candidates.MinBy(candidate => candidate.PricePerBaseUnit);
        }

        var ean = NormalizeEan(product.Ean);
        if (ean is not null && IsValidEan(ean) && candidates.All(candidate =>
                candidate.MatchConfidence == OfferMatchConfidence.ExactEan &&
                string.Equals(NormalizeEan(candidate.Offer.Ean), ean, StringComparison.Ordinal)))
        {
            return candidates.MinBy(candidate => candidate.Offer.OfferPrice);
        }

        return null;
    }

    private static OfferComparisonItem Result(
        ComparableRetailerOffer offer,
        OfferComparisonStatus status,
        OfferMatchConfidence matchConfidence,
        string reason) =>
        new(offer, status, matchConfidence, null, null, reason);

    private static string NormalizeCoreName(string value) =>
        PackageSuffix.Replace(ProductMatcher.Normalize(value), string.Empty).Trim();

    private static string? NormalizeEan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Any(character => !char.IsAsciiDigit(character) && !char.IsWhiteSpace(character) && character != '-'))
        {
            return null;
        }

        var normalized = string.Concat(value.Where(char.IsDigit));
        return normalized.Length == 0 ? null : normalized;
    }

    private static bool IsValidEan(string ean) =>
        ean.Length is 8 or 12 or 13 or 14 && ean.All(char.IsDigit);

    private static bool HasBrandConflict(string? productBrand, string? offerBrand) =>
        !string.IsNullOrWhiteSpace(productBrand) && !string.IsNullOrWhiteSpace(offerBrand) &&
        !string.Equals(ProductMatcher.Normalize(productBrand), ProductMatcher.Normalize(offerBrand), StringComparison.Ordinal);

    private static bool HasPackageConflict(PersonalProduct product, ComparableRetailerOffer offer)
    {
        var productPackage = NormalizePackage(product.PackageQuantity, product.PackageUnit);
        var offerPackage = NormalizePackage(offer.PackageQuantity, offer.PackageUnit);
        if (productPackage is null || offerPackage is null)
        {
            return false;
        }

        return !string.Equals(productPackage.Value.Dimension, offerPackage.Value.Dimension, StringComparison.Ordinal) ||
               productPackage.Value.Quantity != offerPackage.Value.Quantity;
    }

    private static decimal? TryGetPricePerBaseUnit(ComparableRetailerOffer offer, out string? baseUnit)
    {
        var package = NormalizePackage(offer.PackageQuantity, offer.PackageUnit);
        baseUnit = package?.Dimension;
        return package is null ? null : offer.OfferPrice / package.Value.Quantity;
    }

    private static string? GetPackageDimension(decimal? quantity, string? unit) =>
        NormalizePackage(quantity, unit)?.Dimension;

    private static (decimal Quantity, string Dimension)? NormalizePackage(decimal? quantity, string? unit)
    {
        if (quantity is null or <= 0 || string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        var normalizedUnit = unit.Trim().ToLowerInvariant();
        var factor = normalizedUnit switch
        {
            "kg" => 1000m,
            "g" or "ml" or "pc" or "pcs" or "piece" or "pieces" or "stk" => 1m,
            "l" or "liter" or "litre" => 1000m,
            _ => 0m
        };
        var dimension = normalizedUnit switch
        {
            "kg" or "g" => "g",
            "l" or "liter" or "litre" or "ml" => "ml",
            "pc" or "pcs" or "piece" or "pieces" or "stk" => "piece",
            _ => null
        };

        return factor > 0 && dimension is not null
            ? (quantity.Value * factor, dimension)
            : null;
    }
}
