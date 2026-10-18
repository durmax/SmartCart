namespace SmartCart.Web.Models;

public sealed record ComparableRetailerOffer
{
    public string Retailer { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? Ean { get; init; }
    public string? Brand { get; init; }
    public decimal? PackageQuantity { get; init; }
    public string? PackageUnit { get; init; }
    public decimal? OfferPrice { get; init; }
    public decimal? RegularPrice { get; init; }
    public DateTimeOffset? ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public string? SourceUrl { get; init; }
    public bool IsSourceAuthorized { get; init; }
    public bool IsDevelopmentDemo { get; init; }
    public bool IsRestricted { get; init; }
    public string? RestrictionDetails { get; init; }
}
