using System.Net;
using System.Text;
using SmartCart.Web.Services;

namespace SmartCart.Web.Tests;

public sealed class LidlOffersApiClientTests
{
    [Fact]
    public async Task GetOffersAsync_ReturnsApiJsonAndCallsSmartCartApi()
    {
        const string json = "{\"offers\":[{\"title\":\"Offer\"}]}";
        HttpRequestMessage? request = null;
        using var httpClient = CreateHttpClient((message, _) =>
        {
            request = message;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, json));
        });
        var client = new LidlOffersApiClient(httpClient);

        var response = await client.GetOffersAsync("DE5868");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(json, response.Content);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Equal("https://localhost:7297/api/lidl/offers?storeKey=DE5868", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetOffersAsync_PreservesApiErrorStatusAndBody()
    {
        const string error = "{\"error\":\"Lidl returned an unsuccessful response.\",\"upstreamStatusCode\":400}";
        using var httpClient = CreateHttpClient((_, _) => Task.FromResult(
            JsonResponse(HttpStatusCode.BadGateway, error)));
        var client = new LidlOffersApiClient(httpClient);

        var response = await client.GetOffersAsync();

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(error, response.Content);
    }

    [Fact]
    public async Task GetOffersAsync_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromException<HttpResponseMessage>(new OperationCanceledException()));
        var client = new LidlOffersApiClient(httpClient);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetOffersAsync(cancellationToken: cancellation.Token));
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
        new(new StubHttpMessageHandler(sendAsync))
        {
            BaseAddress = new Uri("https://localhost:7297/")
        };

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
    }
}
