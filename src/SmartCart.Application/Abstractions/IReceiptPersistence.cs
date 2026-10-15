using SmartCart.Domain.Entities;

namespace SmartCart.Application.Abstractions;

public interface IReceiptPersistence
{
    Task AddAsync(
        Receipt receipt,
        CancellationToken cancellationToken = default);

    Task<Receipt?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
