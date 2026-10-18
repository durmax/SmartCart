using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public sealed class ShoppingListService(IShoppingListStore store)
{
    public Task<IReadOnlyList<ShoppingListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        store.GetAllAsync(cancellationToken);

    public async Task<ShoppingListItem> AddProductAsync(PersonalProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var currentItems = await store.GetAllAsync(cancellationToken);
        var existingItem = currentItems.FirstOrDefault(item => item.ProductId == product.Id);
        if (existingItem is not null)
        {
            return existingItem;
        }

        var item = new ShoppingListItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = product.UsualQuantity.GetValueOrDefault(1),
            Unit = product.PackageUnit ?? "piece"
        };
        await store.SaveAsync(item, cancellationToken);
        return item;
    }

    public async Task<ShoppingListItem> AddManualAsync(
        string name,
        decimal quantity,
        string? unit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A product name is required.", nameof(name));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var item = new ShoppingListItem
        {
            ProductId = null,
            ProductName = name.Trim(),
            Quantity = quantity,
            Unit = string.IsNullOrWhiteSpace(unit) ? "piece" : unit.Trim()
        };
        await store.SaveAsync(item, cancellationToken);
        return item;
    }

    public async Task UpdateQuantityAsync(Guid id, decimal quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var item = (await store.GetAllAsync(cancellationToken)).FirstOrDefault(candidate => candidate.Id == id)
            ?? throw new KeyNotFoundException("The shopping list item was not found.");
        item.Quantity = quantity;
        await store.SaveAsync(item, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.DeleteAsync(id, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        store.ClearAsync(cancellationToken);
}
