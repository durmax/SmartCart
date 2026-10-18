using SmartCart.Web.Models;
using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class OfferComparisonServiceTests
{
    private static readonly DateTimeOffset Now = new(2030, 5, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly OfferComparisonService _service = new();

    [Fact]
    public void Compare_UsesLowestComparableCurrentUnitPriceForExactEan()
    {
        var product = Product(ean: "4006381333931", packageQuantity: 500, packageUnit: "g");
        var offers = new[]
        {
            Offer("Shop A", "1.00", ean: "4006381333931", packageQuantity: 500, packageUnit: "g"),
            Offer("Shop B", "0.80", ean: "4006381333931", packageQuantity: 500, packageUnit: "g")
        };

        var result = _service.Compare(product, offers, Now);

        Assert.Equal("Shop B", result.BestKnownAvailableOffer?.Offer.Retailer);
        Assert.Equal(OfferComparisonStatus.Current, result.BestKnownAvailableOffer?.Status);
        Assert.Equal(0.0016m, result.BestKnownAvailableOffer?.PricePerBaseUnit);
        Assert.Equal("g", result.BestKnownAvailableOffer?.BaseUnit);
        Assert.Contains("coverage may be incomplete", OfferComparisonResult.BestKnownLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compare_HandlesOfficialUpcomingOfferButDoesNotTreatItAsCurrent()
    {
        var offer = Offer("Shop", "1.00", ean: "4006381333931") with
        {
            ValidFrom = Now.AddDays(2),
            ValidTo = Now.AddDays(5)
        };

        var result = _service.Compare(Product(ean: "4006381333931"), [offer], Now);

        Assert.Equal(OfferComparisonStatus.Upcoming, Assert.Single(result.Offers).Status);
        Assert.Equal(OfferComparisonStatus.Upcoming, result.BestKnownAvailableOffer?.Status);
    }

    [Theory]
    [InlineData(OfferComparisonStatus.Expired, -5, -1)]
    [InlineData(OfferComparisonStatus.UnknownValidity, 0, 0)]
    public void Compare_ExcludesExpiredOrUnknownValidity(OfferComparisonStatus expected, int fromOffset, int toOffset)
    {
        var offer = Offer("Shop", "1.00", ean: "4006381333931") with
        {
            ValidFrom = fromOffset == 0 ? null : Now.AddDays(fromOffset),
            ValidTo = toOffset == 0 ? null : Now.AddDays(toOffset)
        };

        var result = _service.Compare(Product(ean: "4006381333931"), [offer], Now);

        Assert.Equal(expected, Assert.Single(result.Offers).Status);
        Assert.Null(result.BestKnownAvailableOffer);
    }

    [Fact]
    public void Compare_RejectsDemoUnauthenticatedRestrictedAndInvalidPriceData()
    {
        var product = Product(ean: "4006381333931");
        var offers = new[]
        {
            Offer("Demo", "1.00", ean: "4006381333931") with { IsDevelopmentDemo = true },
            Offer("Unverified", "1.00", ean: "4006381333931") with { IsSourceAuthorized = false },
            Offer("Restricted", "1.00", ean: "4006381333931") with { IsRestricted = true },
            Offer("Bad price", "1.00", ean: "4006381333931") with { OfferPrice = 0 },
            Offer("No HTTPS", "1.00", ean: "4006381333931") with { SourceUrl = "http://example.test/offers" }
        };

        var result = _service.Compare(product, offers, Now);

        Assert.Collection(result.Offers,
            item => Assert.Equal(OfferComparisonStatus.DevelopmentDemo, item.Status),
            item => Assert.Equal(OfferComparisonStatus.UnauthorizedSource, item.Status),
            item => Assert.Equal(OfferComparisonStatus.Restricted, item.Status),
            item => Assert.Equal(OfferComparisonStatus.InvalidPrice, item.Status),
            item => Assert.Equal(OfferComparisonStatus.UnauthorizedSource, item.Status));
        Assert.Null(result.BestKnownAvailableOffer);
    }

    [Fact]
    public void Compare_RequiresConservativeNameBrandAndPackageEvidence()
    {
        var product = Product(name: "Brand Pasta 500 g", brand: "Brand", packageQuantity: 500, packageUnit: "g");
        var offers = new[]
        {
            Offer("Unknown brand", "1.00", name: "Brand Pasta 1 kg", brand: null, packageQuantity: 1, packageUnit: "kg"),
            Offer("Wrong brand", "1.00", name: "Brand Pasta 1 kg", brand: "Other", packageQuantity: 1, packageUnit: "kg"),
            Offer("Confirmed different pack", "1.00", name: "Brand Pasta 1 kg", brand: "Brand", packageQuantity: 1, packageUnit: "kg")
        };

        var result = _service.Compare(product, offers, Now);

        Assert.Equal(OfferComparisonStatus.ProductMatchNeedsReview, result.Offers[0].Status);
        Assert.Equal(OfferComparisonStatus.NoProductMatch, result.Offers[1].Status);
        Assert.Equal(OfferComparisonStatus.Current, result.Offers[2].Status);
        Assert.Equal("g", result.Offers[2].BaseUnit);
        Assert.Equal(0.001m, result.Offers[2].PricePerBaseUnit);
        Assert.Equal("Confirmed different pack", result.BestKnownAvailableOffer?.Offer.Retailer);
    }

    [Fact]
    public void Compare_RefusesMismatchedEanAndMismatchedPackageForSameEan()
    {
        var product = Product(ean: "4006381333931", packageQuantity: 500, packageUnit: "g");
        var offers = new[]
        {
            Offer("Different EAN", "1.00", ean: "4006381333932", packageQuantity: 500, packageUnit: "g"),
            Offer("Conflicting package", "1.00", ean: "4006381333931", packageQuantity: 1, packageUnit: "kg")
        };

        var result = _service.Compare(product, offers, Now);

        Assert.Equal(OfferComparisonStatus.NoProductMatch, result.Offers[0].Status);
        Assert.Equal(OfferComparisonStatus.ProductMatchNeedsReview, result.Offers[1].Status);
        Assert.Null(result.BestKnownAvailableOffer);
    }

    [Fact]
    public void Compare_RequiresReviewForMalformedEanInsteadOfFallingBackToName()
    {
        var offer = Offer("Malformed EAN", "1.00", name: "Sample product", ean: "not-an-ean");

        var result = _service.Compare(Product(name: "Sample product"), [offer], Now);

        Assert.Equal(OfferComparisonStatus.ProductMatchNeedsReview, Assert.Single(result.Offers).Status);
        Assert.Null(result.BestKnownAvailableOffer);
    }

    private static PersonalProduct Product(
        string name = "Sample product",
        string? ean = null,
        string? brand = null,
        decimal? packageQuantity = null,
        string? packageUnit = null) =>
        new()
        {
            Name = name,
            NormalizedName = name,
            Ean = ean,
            Brand = brand,
            PackageQuantity = packageQuantity,
            PackageUnit = packageUnit
        };

    private static ComparableRetailerOffer Offer(
        string retailer,
        string price,
        string name = "Sample product",
        string? ean = null,
        string? brand = null,
        decimal? packageQuantity = null,
        string? packageUnit = null) =>
        new()
        {
            Retailer = retailer,
            ProductName = name,
            Ean = ean,
            Brand = brand,
            PackageQuantity = packageQuantity,
            PackageUnit = packageUnit,
            OfferPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture),
            ValidFrom = Now.AddDays(-1),
            ValidTo = Now.AddDays(1),
            SourceUrl = "https://offers.example.test/current",
            IsSourceAuthorized = true
        };
}
