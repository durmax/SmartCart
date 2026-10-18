using SmartCart.Application.Services;
using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public enum ProductSaveStatus
{
    Saved,
    ExistingProductFound,
    ReviewRequired
}

public sealed record ProductSaveResult(ProductSaveStatus Status, PersonalProduct? ExistingProduct = null, IReadOnlyList<PersonalProduct>? Candidates = null);

public sealed class ProductCatalogService(IProductStore store)
{
    public Task<IReadOnlyList<PersonalProduct>> GetAllAsync(CancellationToken cancellationToken = default) =>
        store.GetAllAsync(cancellationToken);

    public async Task<ProductSaveResult> SaveAsync(
        PersonalProduct product,
        bool createSeparateAfterReview = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        product.Name = product.Name?.Trim() ?? string.Empty;
        if (product.Name.Length == 0)
        {
            throw new ArgumentException("A product name is required.", nameof(product));
        }

        var products = await store.GetAllAsync(cancellationToken);
        var candidates = products.Where(existing => existing.Id != product.Id).ToList();
        var match = ProductMatcher.Match(product.Name, candidates.Select(existing => existing.ToDomainProduct()), product.Ean, product.Brand);

        if (match.ConfirmedProduct is not null)
        {
            var existing = candidates.First(product => product.Id == match.ConfirmedProduct.Id);
            return new ProductSaveResult(ProductSaveStatus.ExistingProductFound, existing);
        }

        if (match.Type == ProductMatchType.ReviewRequired && !createSeparateAfterReview)
        {
            var candidateIds = match.Candidates.Select(candidate => candidate.Id).ToHashSet();
            return new ProductSaveResult(
                ProductSaveStatus.ReviewRequired,
                Candidates: candidates.Where(candidate => candidateIds.Contains(candidate.Id)).ToList());
        }

        NormalizeForStorage(product);
        await store.SaveAsync(product, cancellationToken);
        return new ProductSaveResult(ProductSaveStatus.Saved);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.DeleteAsync(id, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        store.ClearAsync(cancellationToken);

    public async Task<string> ExportJsonAsync(CancellationToken cancellationToken = default) =>
        ProductCatalogSerializer.Export(await store.GetAllAsync(cancellationToken));

    public async Task ImportJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        var products = ProductCatalogSerializer.Import(json);
        await store.ReplaceAllAsync(products, cancellationToken);
    }

    private static void NormalizeForStorage(PersonalProduct product)
    {
        if (product.Id == Guid.Empty)
        {
            product.Id = Guid.NewGuid();
        }

        product.Aliases ??= [];
        foreach (var alias in product.Aliases)
        {
            if (alias.Id == Guid.Empty)
            {
                alias.Id = Guid.NewGuid();
            }

        }

        ProductCatalogSerializer.ValidateAndNormalizeProduct(product);
    }
}
