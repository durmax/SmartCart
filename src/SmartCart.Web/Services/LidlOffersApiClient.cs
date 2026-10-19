using System.Net;

namespace SmartCart.Web.Services;

public sealed class LidlOffersApiClient(HttpClient httpClient)
{
    public async Task<LidlOffersApiResponse> GetOffersAsync(
        string storeKey = "DE5868",
        CancellationToken cancellationToken = default)
    {
        var encodedStoreKey = Uri.EscapeDataString(storeKey);
        using var response = await httpClient.GetAsync(
            $"api/lidl/offers?storeKey={encodedStoreKey}",
            cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new LidlOffersApiResponse(response.StatusCode, content);
    }
}

public sealed record LidlOffersApiResponse(HttpStatusCode StatusCode, string Content)
{
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
}
