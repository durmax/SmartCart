using Microsoft.Extensions.DependencyInjection;

namespace SmartCart.Api.Extensions;

public static class SmartCartApiServiceCollectionExtensions
{
    public static IServiceCollection AddSmartCartApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }
}
