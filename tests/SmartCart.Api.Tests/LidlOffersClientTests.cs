using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCart.Api.Lidl;

namespace SmartCart.Api.Tests;

public sealed class LidlOffersClientTests
{
    [Theory]
    [InlineData("DE5868", true)]
    [InlineData("DE1", true)]
    [InlineData("DE", false)]
    [InlineData("de5868", false)]
    [InlineData("DE58A8", false)]
    [InlineData("DE5868/other", false)]
    public void IsValidStoreKey_RequiresDEFollowedByDigits(string storeKey, bool expected)
    {
        Assert.Equal(expected, LidlOffersClient.IsValidStoreKey(storeKey));
    }

    [Fact]
    public async Task GetOffersAsync_ReturnsSuccessfulJsonAndCallsFixedLidlPath()
    {
        const string json = "{\"offers\":[{\"title\":\"Offer\"}]}";
        HttpRequestMessage? request = null;
        using var client = CreateClient((message, _) =>
        {
            request = message;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, json));
        });
        var service = CreateService(client);

        var result = await service.GetOffersAsync("DE5868", CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal((int)HttpStatusCode.OK, result.UpstreamStatusCode);
        Assert.Equal(json, result.Json);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Equal("https://offers.lidlplus.com/app/api/v4/DE/DE5868/offers", request.RequestUri!.AbsoluteUri);
        Assert.Equal(HttpVersion.Version20, request.Version);
        Assert.Equal(HttpVersionPolicy.RequestVersionExact, request.VersionPolicy);
        Assert.Equal("application/json", Assert.Single(request.Headers.Accept).MediaType);
        Assert.Equal("de-DE", Assert.Single(request.Headers.AcceptLanguage).Value);
    }

    [Fact]
    public async Task GetOffersAsync_RejectsInvalidStoreKeyWithoutCallingLidl()
    {
        var requestCount = 0;
        using var client = CreateClient((_, _) =>
        {
            requestCount++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{\"offers\":[]}"));
        });
        var service = CreateService(client);

        var result = await service.GetOffersAsync("DE58A8", CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, requestCount);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, 403)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.TooManyRequests, 429)]
    [InlineData(HttpStatusCode.InternalServerError, 502)]
    public async Task GetOffersAsync_MapsUpstreamErrors(HttpStatusCode upstreamStatus, int expectedStatus)
    {
        using var client = CreateClient((_, _) => Task.FromResult(
            new HttpResponseMessage(upstreamStatus)));
        var service = CreateService(client);

        var result = await service.GetOffersAsync("DE5868", CancellationToken.None);

        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.Equal((int)upstreamStatus, result.UpstreamStatusCode);
        Assert.NotNull(result.Error);
        Assert.Null(result.Json);
    }

    [Fact]
    public async Task GetOffersAsync_MapsTimeoutToGatewayTimeout()
    {
        using var client = CreateClient((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException()));
        var service = CreateService(client);

        var result = await service.GetOffersAsync("DE5868", CancellationToken.None);

        Assert.Equal(504, result.StatusCode);
        Assert.Equal("The request to Lidl timed out.", result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"other\":[]}")]
    [InlineData("{\"offers\":{}}")]
    public async Task GetOffersAsync_RejectsEmptyOrUnexpectedJson(string json)
    {
        using var client = CreateClient((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, json)));
        var service = CreateService(client);

        var result = await service.GetOffersAsync("DE5868", CancellationToken.None);

        Assert.Equal(502, result.StatusCode);
        Assert.Null(result.Json);
        Assert.NotNull(result.Error);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
        new(new StubHttpMessageHandler(sendAsync));

    private static LidlOffersClient CreateService(HttpClient client) =>
        new(client, NullLogger<LidlOffersClient>.Instance);

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
