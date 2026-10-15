using Microsoft.Extensions.DependencyInjection;
using SmartCart.Application.Abstractions;
using SmartCart.Infrastructure.Retailers.Lidl;

namespace SmartCart.Infrastructure.Extensions;

public static class SmartCartInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSmartCartInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRetailerOfferProvider, LidlOfferProvider>();
        return services;
    }
}
