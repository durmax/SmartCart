using SmartCart.Web.Models;
using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class ProductCatalogServiceTests
{
    [Fact]
    public async Task SaveAsync_AddsAndUpdatesProducts_AndDeleteRemovesThem()
    {
        var store = new MemoryProductStore();
        var service = new ProductCatalogService(store);
        var product = new PersonalProduct { Name = "  Milk 1L  ", UsualQuantity = 2 };

        var added = await service.SaveAsync(product);
        var loaded = Assert.Single(await service.GetAllAsync());
        loaded.Name = "Long-life milk 1L";
        await service.SaveAsync(loaded);

        Assert.Equal(ProductSaveStatus.Saved, added.Status);
        Assert.Equal("long life milk 1000ml", loaded.NormalizedName);
        Assert.Equal("Long-life milk 1L", Assert.Single(await service.GetAllAsync()).Name);

        await service.DeleteAsync(loaded.Id);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_PreventsAnExactDuplicate()
    {
        var store = new MemoryProductStore();
        var service = new ProductCatalogService(store);
        var original = new PersonalProduct { Name = "Coffee 500g", Brand = "Jacobs" };
        await service.SaveAsync(original);

        var result = await service.SaveAsync(new PersonalProduct { Name = "Coffee 500 g", Brand = "Jacobs" });

        Assert.Equal(ProductSaveStatus.ExistingProductFound, result.Status);
        Assert.Same(original, result.ExistingProduct);
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_RequiresReviewForAnUncertainCandidate()
    {
        var store = new MemoryProductStore();
        var service = new ProductCatalogService(store);
        await service.SaveAsync(new PersonalProduct { Name = "Jacobs Krönung Kaffee 500g", Brand = "Jacobs" });

        var result = await service.SaveAsync(new PersonalProduct
        {
            Name = "Jacobs Krönung gemahlener Kaffee 0,5kg",
            Brand = "Jacobs"
        });

        Assert.Equal(ProductSaveStatus.ReviewRequired, result.Status);
        Assert.Single(result.Candidates!);
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task ImportAndExport_RoundTripProductsAndAliases()
    {
        var store = new MemoryProductStore();
        var service = new ProductCatalogService(store);
        await service.SaveAsync(new PersonalProduct
        {
            Name = "H-Milch 1L",
            UsualQuantity = 2,
            PackageQuantity = 1,
            PackageUnit = "L",
            Aliases = [new PersonalProductAlias { Alias = "Long-life milk 1 Liter", Retailer = "Lidl" }]
        });

        var json = await service.ExportJsonAsync();
        await service.ClearAsync();
        await service.ImportJsonAsync(json);

        var imported = Assert.Single(await service.GetAllAsync());
        Assert.Equal("H-Milch 1L", imported.Name);
        Assert.Equal("h milch 1000ml", imported.NormalizedName);
        Assert.Equal(2, imported.UsualQuantity);
        Assert.Equal("l", imported.PackageUnit);
        Assert.Equal("Long-life milk 1 Liter", Assert.Single(imported.Aliases).Alias);
        Assert.Contains("schemaVersion", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_RejectsCorruptOrUnsupportedData()
    {
        Assert.Throws<InvalidDataException>(() => ProductCatalogSerializer.Import("not json"));
        Assert.Throws<InvalidDataException>(() => ProductCatalogSerializer.Import("""{"schemaVersion":2,"products":[]}"""));
    }

    private sealed class MemoryProductStore : IProductStore
    {
        private readonly List<PersonalProduct> _products = [];

        public Task<IReadOnlyList<PersonalProduct>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PersonalProduct>>(_products.ToList());

        public Task SaveAsync(PersonalProduct product, CancellationToken cancellationToken = default)
        {
            _products.RemoveAll(existing => existing.Id == product.Id);
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task ReplaceAllAsync(IReadOnlyCollection<PersonalProduct> products, CancellationToken cancellationToken = default)
        {
            _products.Clear();
            _products.AddRange(products);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _products.RemoveAll(product => product.Id == id);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _products.Clear();
            return Task.CompletedTask;
        }
    }
}
