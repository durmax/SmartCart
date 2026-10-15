using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SmartCart.Application.Services;

public sealed class ReceiptService(
    IReceiptImageStorage imageStorage,
    IReceiptPersistence receiptPersistence,
    IOcrService ocrService,
    ILogger<ReceiptService> logger) : IReceiptService
{
    public async Task<CreateReceiptResult> CreateAsync(
        string retailer,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(retailer))
        {
            throw new ArgumentException("Retailer is required.", nameof(retailer));
        }

        ArgumentNullException.ThrowIfNull(imageStream);
        if (!imageStream.CanRead)
        {
            throw new ArgumentException("Image stream must be readable.", nameof(imageStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var imagePath = await imageStorage.SaveAsync(imageStream, fileName, cancellationToken);
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            Retailer = retailer.Trim(),
            ImagePath = imagePath,
            CreatedAt = DateTimeOffset.UtcNow,
            ReceiptNumber = null,
            PurchaseDate = null,
            TotalAmount = null,
            StoreNumber = null,
            RawOcrText = null
        };

        await receiptPersistence.AddAsync(receipt, cancellationToken);
        return ToResult(receipt);
    }

    public async Task<CreateReceiptResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var receipt = await receiptPersistence.GetByIdAsync(id, cancellationToken);
        return receipt is null ? null : ToResult(receipt);
    }

    public async Task<string?> ProcessOcrAsync(
        Guid receiptId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var receipt = await receiptPersistence.GetByIdAsync(receiptId, cancellationToken);
        if (receipt is null)
        {
            return null;
        }

        if (!force && !string.IsNullOrWhiteSpace(receipt.RawOcrText))
        {
            return receipt.RawOcrText;
        }

        if (string.IsNullOrWhiteSpace(receipt.ImagePath))
        {
            throw new InvalidOperationException($"Receipt {receiptId} does not have a stored image path.");
        }

        logger.LogInformation("Starting OCR for Receipt {ReceiptId}", receiptId);
        await using var imageStream = await imageStorage.OpenReadAsync(receipt.ImagePath, cancellationToken);
        var rawOcrText = await ocrService.ExtractTextAsync(imageStream, cancellationToken);
        await receiptPersistence.SaveRawOcrTextAsync(receiptId, rawOcrText, cancellationToken);
        logger.LogInformation("OCR completed for Receipt {ReceiptId}", receiptId);
        logger.LogInformation("OCR returned {CharacterCount} characters", rawOcrText.Length);
        return rawOcrText;
    }

    private static CreateReceiptResult ToResult(Receipt receipt) => new()
    {
        Id = receipt.Id,
        Retailer = receipt.Retailer,
        ImagePath = receipt.ImagePath ?? string.Empty,
        CreatedAt = receipt.CreatedAt
    };
}
