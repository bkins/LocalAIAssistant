using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.ConversationHistory;

namespace LaaUnitTests;

public class ConversationHistoryClientTests
{
    // ── GetHistoryAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ReturnsMappedDtos_WhenApiResponds()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var json      = JsonSerializer.Serialize(new[]
        {
            new { role = "user",      content = "hello",    timestamp }
          , new { role = "assistant", content = "hi there", timestamp }
        });

        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.GetHistoryAsync("conv-123");

        Assert.Equal(2,           result.Count);
        Assert.Equal("user",      result[0].Role);
        Assert.Equal("hello",     result[0].Content);
        Assert.Equal("assistant", result[1].Role);
        Assert.Equal("hi there",  result[1].Content);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyList_WhenApiIsUnreachable()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };

        var sut = new ConversationHistoryClient(client);

        var result = await sut.GetHistoryAsync("conv-123");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyList_WhenApiReturnsNonSuccess()
    {
        var sut = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);

        var result = await sut.GetHistoryAsync("conv-123");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_UsesCorrectRoute_WithConversationIdAndLastParam()
    {
        var recorder = new RecordingHttpMessageHandler();
        var client   = new HttpClient(recorder) { BaseAddress = new Uri("http://localhost/") };
        var sut      = new ConversationHistoryClient(client);

        await sut.GetHistoryAsync("conv-abc", last: 10);

        Assert.NotNull(recorder.LastRequest);
        Assert.Equal("http://localhost/api/conversation/conv-abc/history?last=10"
                   , recorder.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetHistoryAsync_PropagatesCancellation_WhenTokenIsAlreadyCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = BuildClient(HttpStatusCode.OK, "[]");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.GetHistoryAsync("conv-123", ct: cts.Token));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ConversationHistoryClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        return new ConversationHistoryClient(client);
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

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                                   {
                                       Content = new StringContent("[]"
                                                                 , Encoding.UTF8
                                                                 , "application/json")
                                   });
        }
    }
}
