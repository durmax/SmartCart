using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public interface IProductStore
{
    Task<IReadOnlyList<PersonalProduct>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PersonalProduct product, CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IReadOnlyCollection<PersonalProduct> products, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
