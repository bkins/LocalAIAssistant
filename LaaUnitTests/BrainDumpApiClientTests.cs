using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.BrainDump;

namespace LaaUnitTests;

public class BrainDumpApiClientTests
{
    // ── StartSessionAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task StartSessionAsync_ReturnsSession_WhenApiResponds()
    {
        var sessionJson = JsonSerializer.Serialize(new
        {
            id        = "abc123"
          , createdAt = DateTimeOffset.UtcNow
          , updatedAt = DateTimeOffset.UtcNow
          , processed = false
        });

        var sut    = BuildClient(HttpStatusCode.Created, sessionJson);
        var result = await sut.StartSessionAsync();

        Assert.Equal("abc123", result.Id);
        Assert.False(result.Processed);
    }

    [Fact]
    public async Task StartSessionAsync_Throws_WhenApiReturnsError()
    {
        var sut       = BuildClient(HttpStatusCode.InternalServerError, string.Empty);
        var exception = await Record.ExceptionAsync(() => sut.StartSessionAsync());

        Assert.NotNull(exception);
        Assert.IsType<HttpRequestException>(exception);
    }

    // ── GetSessionAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSessionAsync_ReturnsSession_WhenFound()
    {
        var sessionJson = JsonSerializer.Serialize(new
        {
            id         = "s1"
          , avoidance  = "dentist"
          , processed  = false
        });

        var sut    = BuildClient(HttpStatusCode.OK, sessionJson);
        var result = await sut.GetSessionAsync("s1");

        Assert.NotNull(result);
        Assert.Equal("s1",      result!.Id);
        Assert.Equal("dentist", result.Avoidance);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.GetSessionAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetSessionAsync("any");

        Assert.Null(result);
    }

    // ── UpdateCategoryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsUpdatedSession_WhenSuccessful()
    {
        var sessionJson = JsonSerializer.Serialize(new
        {
            id        = "s1"
          , avoidance = "dentist"
        });

        var sut    = BuildClient(HttpStatusCode.OK, sessionJson);
        var result = await sut.UpdateCategoryAsync("s1", BrainDumpCategoryField.Avoidance, "dentist");

        Assert.NotNull(result);
        Assert.Equal("dentist", result!.Avoidance);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenSessionNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.UpdateCategoryAsync("missing", BrainDumpCategoryField.Fears, "text");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.UpdateCategoryAsync("s1", BrainDumpCategoryField.Frustrations, "text");

        Assert.Null(result);
    }

    // ── MarkProcessedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task MarkProcessedAsync_ReturnsProcessedSession_WhenSuccessful()
    {
        var sessionJson = JsonSerializer.Serialize(new
        {
            id          = "s1"
          , processed   = true
          , processedAt = DateTimeOffset.UtcNow
        });

        var sut    = BuildClient(HttpStatusCode.OK, sessionJson);
        var result = await sut.MarkProcessedAsync("s1", "summary", ["task-1"]);

        Assert.NotNull(result);
        Assert.True(result!.Processed);
    }

    [Fact]
    public async Task MarkProcessedAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.MarkProcessedAsync("missing", null, []);

        Assert.Null(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrainDumpApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new BrainDumpApiClient(client);
    }

    private static BrainDumpApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };
        return new BrainDumpApiClient(client);
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
            => Task.FromResult(new HttpResponseMessage(_status)
                               {
                                   Content = new StringContent(_content
                                                             , Encoding.UTF8
                                                             , "application/json")
                               });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
            => throw new HttpRequestException("Simulated network failure");
    }
}
