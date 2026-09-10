using System.Net;
using System.Text;
using LocalAIAssistant.CognitivePlatform.CpClients.MemoryReview;

namespace LaaUnitTests;

public class MemoryReviewApiClientTests
{
    [Fact]
    public async Task GetReviewAsync_MapsReadOnlyReviewProjection()
    {
        var handler = new StaticHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"items":[{"sourceKind":"Identity","sourceId":"identity-1","sourceLabel":"Identity assertion","state":"NeedsReview","content":"Prefers a concise summary","confidence":0.8,"lastReinforcedOrModifiedUtc":"2026-09-09T12:00:00Z","provenance":["User-confirmed statement"],"availableOperations":[]}],"sources":[{"sourceKind":"Identity","isAvailable":true}]}
                """, Encoding.UTF8, "application/json")
        });
        var client = new MemoryReviewApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });

        var result = await client.GetReviewAsync();

        var item = Assert.Single(result!.Items);
        Assert.Equal("Identity", item.SourceKind);
        Assert.Equal("NeedsReview", item.State);
        Assert.Empty(item.AvailableOperations);
        Assert.Equal("/api/memory/review", handler.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenApiIsUnavailable()
    {
        var client = new MemoryReviewApiClient(new HttpClient(
            new StaticHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        {
            BaseAddress = new Uri("http://localhost/")
        });

        var result = await client.GetReviewAsync();

        Assert.Null(result);
    }

    private sealed class StaticHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(response);
        }
    }
}
