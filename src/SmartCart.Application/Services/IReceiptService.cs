using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public interface IReceiptService
{
    Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
