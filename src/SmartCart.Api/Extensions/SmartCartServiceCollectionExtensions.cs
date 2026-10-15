using Microsoft.Extensions.DependencyInjection;
using SmartCart.Application.Abstractions;
using SmartCart.Infrastructure.Retailers.Lidl;

namespace SmartCart.Api.Extensions;

public static class SmartCartServiceCollectionExtensions
{
    public static IServiceCollection AddSmartCartInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRetailerOfferProvider, LidlOfferProvider>();
        return services;
    }
}
