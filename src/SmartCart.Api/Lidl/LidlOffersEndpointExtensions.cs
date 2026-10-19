using Microsoft.AspNetCore.Http;

namespace SmartCart.Api.Lidl;

public static class LidlOffersEndpointExtensions
{
    public static IEndpointRouteBuilder MapLidlOffersTestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/lidl/offers", async (
            string? storeKey,
            LidlOffersClient client,
            CancellationToken cancellationToken) =>
        {
            var result = await client.GetOffersAsync(storeKey ?? "DE5868", cancellationToken);
            if (result.Json is not null)
            {
                return Results.Content(result.Json, "application/json", statusCode: result.StatusCode);
            }

            return Results.Json(
                new LidlOffersError(result.Error!, result.UpstreamStatusCode),
                statusCode: result.StatusCode);
        })
        .WithName("GetLidlOffersPoC");

        return endpoints;
    }

    private sealed record LidlOffersError(string Error, int? UpstreamStatusCode);
}
