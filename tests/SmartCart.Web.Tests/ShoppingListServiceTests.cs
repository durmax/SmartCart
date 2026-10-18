using SmartCart.Web.Models;
using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class ShoppingListServiceTests
{
    [Fact]
    public async Task AddProduct_UsesUsualQuantityButListChangesDoNotMutateCatalogProduct()
    {
        var store = new MemoryShoppingListStore();
        var service = new ShoppingListService(store);
        var product = new PersonalProduct
        {
            Id = Guid.NewGuid(),
            Name = "Milk 1L",
            UsualQuantity = 2,
            PackageUnit = "piece"
        };

        var item = await service.AddProductAsync(product);
        await service.UpdateQuantityAsync(item.Id, 4);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(2, product.UsualQuantity);
        Assert.Equal("Milk 1L", product.Name);
    }

    [Fact]
    public async Task AddProduct_DoesNotAddSameCatalogProductTwice()
    {
        var store = new MemoryShoppingListStore();
        var service = new ShoppingListService(store);
        var product = new PersonalProduct { Id = Guid.NewGuid(), Name = "Coffee", UsualQuantity = 1 };

        var first = await service.AddProductAsync(product);
        var second = await service.AddProductAsync(product);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task AddManualAndDelete_OnlyChangesShoppingList()
    {
        var store = new MemoryShoppingListStore();
        var service = new ShoppingListService(store);
        var manualItem = await service.AddManualAsync("Paper towels", 2, "pack");

        Assert.Null(manualItem.ProductId);
        Assert.Equal("Paper towels", manualItem.ProductName);
        await service.DeleteAsync(manualItem.Id);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task AddManual_RejectsEmptyNamesAndNonPositiveQuantities()
    {
        var service = new ShoppingListService(new MemoryShoppingListStore());

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddManualAsync(" ", 1, "piece"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AddManualAsync("Milk", 0, "piece"));
    }

    private sealed class MemoryShoppingListStore : IShoppingListStore
    {
        private readonly List<ShoppingListItem> _items = [];

        public Task<IReadOnlyList<ShoppingListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListItem>>(_items.ToList());

        public Task SaveAsync(ShoppingListItem item, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(existing => existing.Id == item.Id);
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }
    }
}
