namespace SmartCart.Web.Models;

public enum OfferComparisonStatus
{
    Current,
    Upcoming,
    DevelopmentDemo,
    UnauthorizedSource,
    Restricted,
    InvalidPrice,
    UnknownValidity,
    InvalidValidity,
    Expired,
    NoProductMatch,
    ProductMatchNeedsReview
}

public enum OfferMatchConfidence
{
    ExactEan,
    Confirmed,
    NeedsReview,
    NoMatch
}

public sealed record OfferComparisonItem(
    ComparableRetailerOffer Offer,
    OfferComparisonStatus Status,
    OfferMatchConfidence MatchConfidence,
    decimal? PricePerBaseUnit,
    string? BaseUnit,
    string Reason);

public sealed record OfferComparisonResult(
    IReadOnlyList<OfferComparisonItem> Offers,
    OfferComparisonItem? BestKnownAvailableOffer)
{
    public const string BestKnownLabel = "Best known available offer; source coverage may be incomplete.";
}
