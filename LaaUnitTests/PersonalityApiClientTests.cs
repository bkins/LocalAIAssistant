using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.Personality;

namespace LaaUnitTests;

public class PersonalityApiClientTests
{
    // ── GetPersonalitiesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPersonalitiesAsync_ReturnsMappedDtos_WhenApiResponds()
    {
        var id   = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new[]
        {
            new
            {
                id          = id
              , name        = "Friendly"
              , description = "Helpful AI"
              , systemPrompt = "Be nice."
              , isBuiltIn   = true
              , isActive    = true
              , modelConfig = (object?)null
            }
        });

        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.GetPersonalitiesAsync();

        Assert.Single(result);
        Assert.Equal(id,         result[0].Id);
        Assert.Equal("Friendly", result[0].Name);
        Assert.True(result[0].IsActive);
        Assert.True(result[0].IsBuiltIn);
    }

    [Fact]
    public async Task GetPersonalitiesAsync_ReturnsEmptyList_WhenApiIsUnreachable()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };

        var sut = new PersonalityApiClient(client);

        var result = await sut.GetPersonalitiesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPersonalitiesAsync_ReturnsEmptyList_WhenApiReturnsNonSuccess()
    {
        var sut = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);

        var result = await sut.GetPersonalitiesAsync();

        Assert.Empty(result);
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_SendsPutRequest_ToCorrectRoute()
    {
        var recorder = new RecordingHttpMessageHandler();
        var client   = new HttpClient(recorder) { BaseAddress = new Uri("http://localhost/") };
        var sut      = new PersonalityApiClient(client);
        var id       = Guid.NewGuid();

        await sut.ActivateAsync(id);

        Assert.NotNull(recorder.LastRequest);
        Assert.Equal(HttpMethod.Put,                                 recorder.LastRequest!.Method);
        Assert.Equal($"http://localhost/api/personality/{id}/activate", recorder.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task ActivateAsync_DoesNotThrow_WhenApiIsUnreachable()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };

        var sut = new PersonalityApiClient(client);

        var exception = await Record.ExceptionAsync(() => sut.ActivateAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PersonalityApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        return new PersonalityApiClient(client);
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage  request
                                                             , CancellationToken   ct)
            => Task.FromResult(new HttpResponseMessage(_status)
                               {
                                   Content = new StringContent(_content
                                                             , Encoding.UTF8
                                                             , "application/json")
                               });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage  request
                                                             , CancellationToken   ct)
            => throw new HttpRequestException("Simulated network failure");
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage  request
                                                             , CancellationToken   ct)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
