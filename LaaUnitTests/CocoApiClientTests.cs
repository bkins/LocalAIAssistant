using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.Coco;

namespace LaaUnitTests;

public class CocoApiClientTests
{
    // ── AskAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AskAsync_ReturnsBody_WhenApiResponds()
    {
        var sut    = BuildClient(HttpStatusCode.OK, "The answer to your question.");
        var result = await sut.AskAsync("What does UserService do?");

        Assert.NotNull(result);
        Assert.Equal("The answer to your question.", result);
    }

    [Fact]
    public async Task AskAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.AskAsync("anything");

        Assert.Null(result);
    }

    [Fact]
    public async Task AskAsync_ReturnsNull_WhenBodyIsWhitespace()
    {
        var sut    = BuildClient(HttpStatusCode.OK, "   ");
        var result = await sut.AskAsync("anything");

        Assert.Null(result);
    }

    [Fact]
    public async Task AskAsync_Throws_WhenCancelled()
    {
        var sut       = BuildClient(HttpStatusCode.OK, "body");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.AskAsync("anything", cts.Token));
    }

    // ── GetHealthAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHealthAsync_ReturnsData_WhenApiResponds()
    {
        var json = JsonSerializer.Serialize(new
        {
            success = true
          , data    = new
                      {
                          apiAvailable      = true
                        , ollamaAvailable   = true
                        , storageAvailable  = true
                        , indexingAvailable = true
                        , message           = "All systems go"
                      }
        });

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetHealthAsync();

        Assert.NotNull(result);
        Assert.True(result!.ApiAvailable);
        Assert.True(result.OllamaAvailable);
        Assert.Equal("All systems go", result.Message);
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetHealthAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsNull_WhenServerErrors()
    {
        var sut    = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);
        var result = await sut.GetHealthAsync();

        Assert.Null(result);
    }

    // ── GetStatsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_ReturnsData_WhenApiResponds()
    {
        var now  = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(new
        {
            success = true
          , data    = new { totalChunks = 120, uniqueFiles = 15, lastUpdated = now }
        });

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetStatsAsync();

        Assert.NotNull(result);
        Assert.Equal(120, result!.TotalChunks);
        Assert.Equal(15,  result.UniqueFiles);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetStatsAsync();

        Assert.Null(result);
    }

    // ── GetStatusAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_ReturnsConnectedStatus_WhenApiAndStatsRespond()
    {
        var healthJson = JsonSerializer.Serialize(new
        {
            success = true
          , data    = new { apiAvailable = true, ollamaAvailable = true, storageAvailable = true, indexingAvailable = true }
        });
        var statsJson = JsonSerializer.Serialize(new
        {
            success = true
          , data    = new { totalChunks = 50, uniqueFiles = 8, lastUpdated = (DateTime?)null }
        });

        var responses = new Queue<(HttpStatusCode, string)>(new[]
        {
            (HttpStatusCode.OK, healthJson)
          , (HttpStatusCode.OK, statsJson)
        });
        var sut    = BuildSequentialClient(responses);
        var result = await sut.GetStatusAsync();

        Assert.True(result.IsReachable);
        Assert.True(result.OllamaAvailable);
        Assert.Equal(50, result.TotalChunks);
        Assert.Equal(8,  result.UniqueFiles);
        Assert.Contains("Connected", result.Summary);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsUnreachableStatus_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetStatusAsync();

        Assert.False(result.IsReachable);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unavailable", result.Summary);
    }

    // ── GetModelsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModelsAsync_ReturnsList_WhenApiResponds()
    {
        var json = JsonSerializer.Serialize(new
        {
            models = new[]
            {
                new { name = "nomic-embed-text", model = "nomic-embed-text:latest" }
              , new { name = "llama3.2",         model = "llama3.2:3b" }
            }
        });

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetModelsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Contains("nomic-embed-text", result);
        Assert.Contains("llama3.2",         result);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetModelsAsync();

        Assert.Null(result);
    }

    // ── IndexPathAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IndexPathAsync_CompletesWithoutThrow_WhenApiResponds()
    {
        var sut = BuildClient(HttpStatusCode.OK, string.Empty);

        var exception = await Record.ExceptionAsync(
            () => sut.IndexPathAsync(@"C:\source\repos\MyProject"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task IndexPathAsync_CompletesWithoutThrow_WhenNetworkFails()
    {
        var sut = BuildThrowingClient();

        var exception = await Record.ExceptionAsync(
            () => sut.IndexPathAsync(@"C:\source\repos\MyProject"));

        Assert.Null(exception);
    }

    // ── AskStreamAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AskStreamAsync_YieldsCompleteEvent_WhenApiStreamsValidSse()
    {
        const string sse = "data: {\"stage\":\"complete\",\"response\":\"Here is the answer.\",\"timestamp\":\"2026-06-02T00:00:00Z\"}\n\n";

        var sut    = BuildClient(HttpStatusCode.OK, sse, contentType: "text/event-stream");
        var events = new List<LocalAIAssistant.Core.Coco.Models.CocoAskEvent>();

        await foreach (var ev in sut.AskStreamAsync("What is X?"))
            events.Add(ev);

        Assert.Single(events);
        Assert.True(events[0].IsComplete);
        Assert.Equal("Here is the answer.", events[0].Response);
    }

    [Fact]
    public async Task AskStreamAsync_YieldsNothing_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var events = new List<LocalAIAssistant.Core.Coco.Models.CocoAskEvent>();

        await foreach (var ev in sut.AskStreamAsync("anything"))
            events.Add(ev);

        Assert.Empty(events);
    }

    [Fact]
    public async Task AskStreamAsync_SkipsHeartbeatEvents()
    {
        const string sse =
            "data: {\"status\":\"Heartbeat\",\"timestamp\":\"2026-06-02T00:00:00Z\"}\n\n"
          + "data: {\"stage\":\"complete\",\"response\":\"Done.\",\"timestamp\":\"2026-06-02T00:00:00Z\"}\n\n";

        var sut    = BuildClient(HttpStatusCode.OK, sse, contentType: "text/event-stream");
        var events = new List<LocalAIAssistant.Core.Coco.Models.CocoAskEvent>();

        await foreach (var ev in sut.AskStreamAsync("anything"))
            events.Add(ev);

        Assert.Single(events);
        Assert.True(events[0].IsComplete);
    }

    // ── IndexStreamAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task IndexStreamAsync_YieldsCompletedEvent_WhenApiStreamsValidSse()
    {
        const string sse = "data: {\"status\":\"Completed\",\"message\":\"Index done\",\"processed\":10,\"total\":10,\"timestamp\":\"2026-06-02T00:00:00Z\"}\n\n";

        var sut    = BuildClient(HttpStatusCode.OK, sse, contentType: "text/event-stream");
        var events = new List<LocalAIAssistant.Core.Coco.Models.CocoIndexEvent>();

        await foreach (var ev in sut.IndexStreamAsync(@"C:\source"))
            events.Add(ev);

        Assert.Single(events);
        Assert.True(events[0].IsCompleted);
        Assert.Equal(10, events[0].Processed);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CocoApiClient BuildClient( HttpStatusCode status
                                            , string         content
                                            , string         contentType = "application/json" )
    {
        var handler = new StubHttpMessageHandler(status, content, contentType);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5292/") };
        return new CocoApiClient(client);
    }

    private static CocoApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost:5292/")
                     };
        return new CocoApiClient(client);
    }

    private static CocoApiClient BuildSequentialClient(Queue<(HttpStatusCode, string)> responses)
    {
        var handler = new SequentialHttpMessageHandler(responses);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5292/") };
        return new CocoApiClient(client);
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;
        private readonly string         _contentType;

        public StubHttpMessageHandler(HttpStatusCode status, string content, string contentType = "application/json")
        {
            _status      = status;
            _content     = content;
            _contentType = contentType;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(_status)
                                   {
                                       Content = new StringContent(_content, Encoding.UTF8, _contentType)
                                   });
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
            => throw new HttpRequestException("Simulated network failure");
    }

    private sealed class SequentialHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Content)> _responses;

        public SequentialHttpMessageHandler(Queue<(HttpStatusCode, string)> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
        {
            ct.ThrowIfCancellationRequested();

            if (_responses.Count == 0)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var (status, content) = _responses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(status)
                                   {
                                       Content = new StringContent(content, Encoding.UTF8, "application/json")
                                   });
        }
    }
}
