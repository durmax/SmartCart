using Microsoft.EntityFrameworkCore;
using SmartCart.Application.Abstractions;
using SmartCart.Domain.Entities;

namespace SmartCart.Infrastructure.Persistence;

public sealed class ReceiptPersistence(SmartCartDbContext dbContext) : IReceiptPersistence
{
    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        await dbContext.Receipts.AddAsync(receipt, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Receipts
            .AsNoTracking()
            .FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);
    }

    public Task<Receipt?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Receipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Adjustments)
            .FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);
    }

    public async Task SaveRawOcrTextAsync(
        Guid id,
        string rawOcrText,
        CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.Receipts
            .FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Receipt {id} was not found.");

        receipt.RawOcrText = rawOcrText;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveParsedReceiptAsync(
        Receipt receipt,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var existingReceipt = await dbContext.Receipts
            .Include(existing => existing.Items)
            .ThenInclude(item => item.Adjustments)
            .FirstOrDefaultAsync(existing => existing.Id == receipt.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Receipt {receipt.Id} was not found.");

        if (existingReceipt.Items.Count > 0 && !force)
        {
            return;
        }

        if (force)
        {
            // Remove adjustments first to avoid FK issues, then remove items.
            var adjustments = existingReceipt.Items.SelectMany(i => i.Adjustments).ToList();
            if (adjustments.Count > 0)
            {
                dbContext.ReceiptItemAdjustments.RemoveRange(adjustments);
            }

            if (existingReceipt.Items.Count > 0)
            {
                dbContext.ReceiptItems.RemoveRange(existingReceipt.Items);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        existingReceipt.ReceiptNumber = receipt.ReceiptNumber;
        existingReceipt.PurchaseDate = receipt.PurchaseDate;
        existingReceipt.TotalAmount = receipt.TotalAmount;

        // Add new items via DbSet to ensure EF treats them as new inserts.
        var newItems = receipt.Items.Select(i => new ReceiptItem
        {
            Id = Guid.NewGuid(),
            ReceiptId = existingReceipt.Id,
            ProductId = i.ProductId,
            OriginalText = i.OriginalText,
            ParsedName = i.ParsedName,
            Unit = i.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice,
            Confidence = i.Confidence
        }).ToList();

        await dbContext.ReceiptItems.AddRangeAsync(newItems, cancellationToken);

        var newAdjustments = new List<ReceiptItemAdjustment>();
        foreach (var srcItem in receipt.Items)
        {
            var created = newItems.First(n => n.OriginalText == srcItem.OriginalText && n.TotalPrice == srcItem.TotalPrice);
            foreach (var adj in srcItem.Adjustments)
            {
                newAdjustments.Add(new ReceiptItemAdjustment
                {
                    Id = Guid.NewGuid(),
                    ReceiptItemId = created.Id,
                    Description = adj.Description,
                    Amount = adj.Amount
                });
            }
        }

        if (newAdjustments.Count > 0)
        {
            await dbContext.ReceiptItemAdjustments.AddRangeAsync(newAdjustments, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
