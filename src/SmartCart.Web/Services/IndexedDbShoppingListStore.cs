using Microsoft.JSInterop;
using SmartCart.Web.Models;

namespace SmartCart.Web.Services;

public sealed class IndexedDbShoppingListStore(IJSRuntime jsRuntime) : IShoppingListStore, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<IReadOnlyList<ShoppingListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        return await module.InvokeAsync<List<ShoppingListItem>>("getShoppingItems", cancellationToken) ?? [];
    }

    public async Task SaveAsync(ShoppingListItem item, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("saveShoppingItem", cancellationToken, item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("deleteShoppingItem", cancellationToken, id);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync(cancellationToken);
        await module.InvokeVoidAsync("clearShoppingItems", cancellationToken);
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
