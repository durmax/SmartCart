using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartCart.Application.Abstractions;
using SmartCart.Application.Services;
using SmartCart.Infrastructure.Persistence;
using SmartCart.Infrastructure.Retailers.Lidl;
using SmartCart.Infrastructure.Storage;

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
        var storagePath = configuration["ReceiptStorage:Path"] ?? "receipts";
        var contentRootPath = configuration["ReceiptStorage:ContentRoot"]
            ?? configuration["ContentRoot"]
            ?? Directory.GetCurrentDirectory();
        services.AddSingleton<IReceiptImageStorage>(
            _ => new LocalReceiptImageStorage(storagePath, contentRootPath));
        services.AddScoped<IReceiptPersistence, ReceiptPersistence>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IRetailerOfferProvider, LidlOfferProvider>();
        return services;
    }
}
