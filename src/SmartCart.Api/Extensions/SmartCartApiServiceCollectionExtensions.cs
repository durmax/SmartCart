using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartCart.Api.Lidl;

namespace SmartCart.Api.Extensions;

public static class SmartCartApiServiceCollectionExtensions
{
    public const string DevelopmentCorsPolicy = "SmartCartWebDevelopment";

    public static IServiceCollection AddSmartCartApi(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(DevelopmentCorsPolicy, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        }));

        services.AddHttpClient<LidlOffersClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
