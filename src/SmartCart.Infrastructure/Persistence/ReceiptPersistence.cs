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
            dbContext.ReceiptItems.RemoveRange(existingReceipt.Items);
        }

        existingReceipt.ReceiptNumber = receipt.ReceiptNumber;
        existingReceipt.PurchaseDate = receipt.PurchaseDate;
        existingReceipt.TotalAmount = receipt.TotalAmount;
        foreach (var item in receipt.Items)
        {
            existingReceipt.Items.Add(item);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
