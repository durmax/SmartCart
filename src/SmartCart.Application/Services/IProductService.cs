using SmartCart.Domain.Entities;

namespace SmartCart.Application.Services;

public interface IProductService
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
