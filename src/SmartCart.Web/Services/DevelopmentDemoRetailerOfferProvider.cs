using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;

namespace SmartCart.Web.Services;

public sealed class DevelopmentDemoRetailerOfferProvider(string retailer) : IRetailerOfferProvider
{
    private readonly string _retailer = string.IsNullOrWhiteSpace(retailer)
        ? throw new ArgumentException("A retailer name is required.", nameof(retailer))
        : retailer;

    public string Retailer => $"{_retailer} (demo)";

    public Task<IReadOnlyCollection<Offer>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<Offer> offers =
        [
            new Offer
            {
                Id = Guid.NewGuid(),
                Retailer = Retailer,
                Name = "Sample pantry item (500 g)",
                OfferPrice = 1.29m,
                FetchedAt = DateTimeOffset.UtcNow
            },
            new Offer
            {
                Id = Guid.NewGuid(),
                Retailer = Retailer,
                Name = "Sample fruit (1 kg)",
                OfferPrice = 2.49m,
                FetchedAt = DateTimeOffset.UtcNow
            }
        ];

        return Task.FromResult(offers);
    }
}
