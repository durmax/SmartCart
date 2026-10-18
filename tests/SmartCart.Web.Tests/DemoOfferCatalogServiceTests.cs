using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;
using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class DemoOfferCatalogServiceTests
{
    [Fact]
    public async Task GetOffers_ReturnsExplicitlyLabeledDemoOnlyData()
    {
        var service = new DemoOfferCatalogService(
        [
            new DevelopmentDemoRetailerOfferProvider("Lidl"),
            new DevelopmentDemoRetailerOfferProvider("Aldi Nord")
        ]);

        var offers = await service.GetOffersAsync();

        Assert.Contains("demo", DemoOfferCatalogService.SourceLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not actual retailer offers", DemoOfferCatalogService.SourceLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not fetched, verified", DemoOfferCatalogService.Limitation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, offers.Count);
        Assert.Contains(offers, offer => offer.Retailer == "Lidl (demo)");
        Assert.Contains(offers, offer => offer.Retailer == "Aldi Nord (demo)");
        Assert.All(offers, offer =>
        {
            Assert.Null(offer.SourceUrl);
            Assert.Null(offer.ValidFrom);
            Assert.Null(offer.ValidTo);
        });
    }

    [Fact]
    public async Task GetOffers_ThrowsWhenCancellationRequested()
    {
        var provider = new DevelopmentDemoRetailerOfferProvider("Lidl");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetOffersAsync(cancellation.Token));
    }
}
