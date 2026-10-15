using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public sealed class ReceiptService(
    IReceiptImageStorage imageStorage,
    IReceiptPersistence receiptPersistence) : IReceiptService
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

    private static CreateReceiptResult ToResult(Receipt receipt) => new()
    {
        Id = receipt.Id,
        Retailer = receipt.Retailer,
        ImagePath = receipt.ImagePath ?? string.Empty,
        CreatedAt = receipt.CreatedAt
    };
}
