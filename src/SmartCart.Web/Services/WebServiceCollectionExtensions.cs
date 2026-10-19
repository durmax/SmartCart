using Microsoft.Extensions.DependencyInjection;
using SmartCart.Application.Abstractions;
using SmartCart.Application.Receipts.Lidl;

namespace SmartCart.Web.Services;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddSmartCartWeb(this IServiceCollection services, string baseAddress)
    {
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });
        services.AddScoped<IReceiptParser, LidlReceiptParser>();
        services.AddScoped<IRetailerOfferProvider>(_ => new DevelopmentDemoRetailerOfferProvider("Lidl"));
        services.AddScoped<IRetailerOfferProvider>(_ => new DevelopmentDemoRetailerOfferProvider("ALDI SÜD"));
        services.AddScoped<DemoOfferCatalogService>();
        services.AddScoped<OfferComparisonService>();
        services.AddScoped<IProductStore, IndexedDbProductStore>();
        services.AddScoped<ProductCatalogService>();
        services.AddScoped<IShoppingListStore, IndexedDbShoppingListStore>();
        services.AddScoped<ShoppingListService>();
        return services;
    }
}
