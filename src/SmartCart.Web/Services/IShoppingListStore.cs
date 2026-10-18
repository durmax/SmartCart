using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public interface IShoppingListStore
{
    Task<IReadOnlyList<ShoppingListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ShoppingListItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
