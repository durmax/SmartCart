using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SmartCart.Api.Lidl;

public sealed class LidlOffersClient(HttpClient httpClient, ILogger<LidlOffersClient> logger)
{
    public static bool IsValidStoreKey(string? storeKey) =>
        storeKey is { Length: > 2 } &&
        storeKey.StartsWith("DE", StringComparison.Ordinal) &&
        storeKey.AsSpan(2).IndexOfAnyExceptInRange('0', '9') < 0;

    public async Task<LidlOffersResult> GetOffersAsync(string storeKey, CancellationToken cancellationToken)
    {
        if (!IsValidStoreKey(storeKey))
        {
            return new LidlOffersResult(StatusCodes.Status400BadRequest, null,
                "Store key must start with DE and contain digits only.", null);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://offers.lidlplus.com/app/api/v4/DE/{storeKey}/offers")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.AcceptLanguage.ParseAdd("de-DE");

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Lidl offers request completed with upstream status {UpstreamStatusCode} in {DurationMilliseconds} ms",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var (statusCode, error) = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => (StatusCodes.Status403Forbidden, "Lidl denied access to offers."),
                    HttpStatusCode.NotFound => (StatusCodes.Status404NotFound, "Lidl offers were not found for this store."),
                    HttpStatusCode.TooManyRequests => (StatusCodes.Status429TooManyRequests, "Lidl rate-limited the request."),
                    _ => (StatusCodes.Status502BadGateway, "Lidl returned an unsuccessful response.")
                };

                return new LidlOffersResult(statusCode, null, error, (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json) || !HasOffersArray(json))
            {
                return new LidlOffersResult(StatusCodes.Status502BadGateway, null,
                    "Lidl returned an empty or unexpected JSON response.", (int)response.StatusCode);
            }

            return new LidlOffersResult(StatusCodes.Status200OK, json, null, (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            logger.LogWarning("Lidl offers request timed out after {DurationMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return new LidlOffersResult(StatusCodes.Status504GatewayTimeout, null,
                "The request to Lidl timed out.", null);
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            logger.LogWarning("Lidl offers request failed after {DurationMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return new LidlOffersResult(StatusCodes.Status502BadGateway, null,
                "Lidl could not be reached.", null);
        }
    }

    private static bool HasOffersArray(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("offers", out var offers) &&
                offers.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record LidlOffersResult(int StatusCode, string? Json, string? Error, int? UpstreamStatusCode);
