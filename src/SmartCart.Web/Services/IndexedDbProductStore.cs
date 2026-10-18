using Microsoft.JSInterop;
using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public sealed class IndexedDbProductStore(IJSRuntime jsRuntime) : IProductStore, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<IReadOnlyList<PersonalProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        return await module.InvokeAsync<List<PersonalProduct>>("getAllProducts", cancellationToken) ?? [];
    }

    public async Task SaveAsync(PersonalProduct product, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("saveProduct", cancellationToken, product);
    }

    public async Task ReplaceAllAsync(IReadOnlyCollection<PersonalProduct> products, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("replaceAllProducts", cancellationToken, products);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("deleteProduct", cancellationToken, id);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("clearProducts", cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./js/indexedDbCatalog.js");
        return _module;
    }
}
