using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartCart.Application.Abstractions;
using SmartCart.Infrastructure.Persistence;
using SmartCart.Infrastructure.Retailers.Lidl;

namespace SmartCart.Infrastructure.Extensions;

public static class SmartCartInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSmartCartInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SmartCartDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'SmartCartDatabase' was not found.");

        services.AddDbContext<SmartCartDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IRetailerOfferProvider, LidlOfferProvider>();
        return services;
    }
}
