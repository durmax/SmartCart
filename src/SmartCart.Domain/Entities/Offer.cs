namespace SmartCart.Domain.Entities;

public class Offer
{
    public Guid Id { get; set; }

    public string Retailer { get; set; } = string.Empty;

    public string? ExternalProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public decimal? RegularPrice { get; set; }

    public decimal? OfferPrice { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    public string? SourceUrl { get; set; }

    public DateTimeOffset FetchedAt { get; set; }
}
