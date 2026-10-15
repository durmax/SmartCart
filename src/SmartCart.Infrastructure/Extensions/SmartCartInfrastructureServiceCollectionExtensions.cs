using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartCart.Application.Abstractions;
using SmartCart.Application.Services;
using SmartCart.Infrastructure.Persistence;
using SmartCart.Infrastructure.OCR;
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
        services.Configure<OcrOptions>(options =>
        {
            options.TessDataPath = configuration[$"{OcrOptions.SectionName}:TessDataPath"] ?? options.TessDataPath;
            options.Language = configuration[$"{OcrOptions.SectionName}:Language"] ?? options.Language;
        });
        services.AddScoped<IOcrService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OcrOptions>>().Value;
            var dataPath = Path.GetFullPath(options.TessDataPath, AppContext.BaseDirectory);
            return new TesseractOcrService(dataPath, options.Language);
        });
        services.AddScoped<IRetailerOfferProvider, LidlOfferProvider>();
        return services;
    }
}
