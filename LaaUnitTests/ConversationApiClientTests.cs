using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.ConversationHistory;

namespace LaaUnitTests;

public class ConversationApiClientTests
{
    // ── GetAllConversationsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllConversationsAsync_ReturnsMappedDtos_WhenApiResponds()
    {
        var now  = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(new object[]
        {
            new { conversationId = "conv-1", name = "Morning chat", lastActiveUtc = now, messageCount = 4 }
          , new { conversationId = "conv-2", name = (string?)null,  lastActiveUtc = now, messageCount = 2 }
        });

        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.GetAllConversationsAsync();

        Assert.Equal(2,              result.Count);
        Assert.Equal("conv-1",       result[0].ConversationId);
        Assert.Equal("Morning chat", result[0].Name);
        Assert.Equal(4,              result[0].MessageCount);
        Assert.Equal("conv-2",       result[1].ConversationId);
        Assert.Null(result[1].Name);
    }

    [Fact]
    public async Task GetAllConversationsAsync_ReturnsEmptyList_WhenApiIsUnreachable()
    {
        var sut = BuildThrowingClient();

        var result = await sut.GetAllConversationsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllConversationsAsync_ReturnsEmptyList_WhenApiReturnsNonSuccess()
    {
        var sut = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);

        var result = await sut.GetAllConversationsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllConversationsAsync_UsesCorrectRoute()
    {
        var recorder = new RecordingHttpMessageHandler();
        var client   = new HttpClient(recorder) { BaseAddress = new Uri("http://localhost/") };
        var sut      = new ConversationApiClient(client);

        await sut.GetAllConversationsAsync();

        Assert.NotNull(recorder.LastRequest);
        Assert.Equal("http://localhost/api/conversation"
                   , recorder.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetAllConversationsAsync_PropagatesCancellation_WhenTokenIsAlreadyCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = BuildClient(HttpStatusCode.OK, "[]");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.GetAllConversationsAsync(cts.Token));
    }

    // ── DeleteConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversationAsync_ReturnsTrue_WhenApiReturns204()
    {
        var sut = BuildClient(HttpStatusCode.NoContent, string.Empty);

        var result = await sut.DeleteConversationAsync("conv-del");

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteConversationAsync_ReturnsFalse_WhenApiReturns404()
    {
        var sut = BuildClient(HttpStatusCode.NotFound, string.Empty);

        var result = await sut.DeleteConversationAsync("conv-ghost");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteConversationAsync_ReturnsFalse_WhenApiIsUnreachable()
    {
        var sut = BuildThrowingClient();

        var result = await sut.DeleteConversationAsync("conv-unreachable");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteConversationAsync_UsesCorrectRoute()
    {
        var recorder = new RecordingHttpMessageHandler(HttpStatusCode.NoContent);
        var client   = new HttpClient(recorder) { BaseAddress = new Uri("http://localhost/") };
        var sut      = new ConversationApiClient(client);

        await sut.DeleteConversationAsync("conv-abc");

        Assert.NotNull(recorder.LastRequest);
        Assert.Equal(HttpMethod.Delete,                                          recorder.LastRequest!.Method);
        Assert.Equal("http://localhost/api/conversation/conv-abc", recorder.LastRequest.RequestUri!.ToString());
    }

    // ── RenameConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RenameConversationAsync_DoesNotThrow_WhenApiIsUnreachable()
    {
        var sut = BuildThrowingClient();

        var ex = await Record.ExceptionAsync(() => sut.RenameConversationAsync("conv-id", "New Name"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task RenameConversationAsync_UsesCorrectRoute()
    {
        var recorder = new RecordingHttpMessageHandler();
        var client   = new HttpClient(recorder) { BaseAddress = new Uri("http://localhost/") };
        var sut      = new ConversationApiClient(client);

        await sut.RenameConversationAsync("conv-abc", "My Chat");

        Assert.NotNull(recorder.LastRequest);
        Assert.Equal(HttpMethod.Put,                                                       recorder.LastRequest!.Method);
        Assert.Equal("http://localhost/api/conversation/conv-abc/name", recorder.LastRequest.RequestUri!.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ConversationApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        return new ConversationApiClient(client);
    }

    private static ConversationApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };

        return new ConversationApiClient(client);
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
        private readonly HttpStatusCode _status;

        public HttpRequestMessage? LastRequest { get; private set; }

        public RecordingHttpMessageHandler(HttpStatusCode status = HttpStatusCode.OK)
        {
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(_status)
                                   {
                                       Content = new StringContent("[]"
                                                                 , Encoding.UTF8
                                                                 , "application/json")
                                   });
        }
    }
}
