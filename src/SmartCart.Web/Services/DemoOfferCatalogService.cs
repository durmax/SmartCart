using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;

namespace SmartCart.Web.Services;

public sealed class DemoOfferCatalogService(IEnumerable<IRetailerOfferProvider> providers)
{
    public const string SourceLabel = "Development demo data — not actual retailer offers";
    public const string Limitation = "Sample products and prices are illustrative only. They are not fetched, verified, or valid retailer offers.";

    public async Task<IReadOnlyCollection<Offer>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        var offers = new List<Offer>();
        foreach (var provider in providers)
        {
            offers.AddRange(await provider.GetOffersAsync(cancellationToken));
        }

        return offers;
    }
}
