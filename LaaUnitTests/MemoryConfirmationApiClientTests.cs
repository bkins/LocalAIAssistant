using System.Net;
using System.Net.Http.Json;
using System.Text;
using LocalAIAssistant.CognitivePlatform.CpClients.Personas;

namespace LaaUnitTests;

public class MemoryConfirmationApiClientTests
{
    [Fact]
    public async Task RefreshAsync_ReturnsPendingCount_ForActivePersonaConversation()
    {
        var personaId = Guid.NewGuid();
        var handler = new SequencedHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(personaId)
            }
          , new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{},{},{}]", Encoding.UTF8, "application/json")
            });
        var client = new MemoryConfirmationApiClient(new HttpClient(handler)
                                                      {
                                                          BaseAddress = new Uri("http://localhost/")
                                                      });

        var result = await client.RefreshAsync("conversation one");

        Assert.True(result.IsAuthoritative);
        Assert.Equal(3, result.PendingCount);
        Assert.Equal("/api/persona/active/conversation%20one", handler.RequestUris[0].AbsolutePath + handler.RequestUris[0].Query);
        Assert.Equal($"/api/persona/{personaId}/memory/pending?conversationId=conversation%20one", handler.RequestUris[1].AbsolutePath + handler.RequestUris[1].Query);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsKnownZero_WhenConversationHasNoActivePersona()
    {
        var handler = new SequencedHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new MemoryConfirmationApiClient(new HttpClient(handler)
                                                      {
                                                          BaseAddress = new Uri("http://localhost/")
                                                      });

        var result = await client.RefreshAsync("conversation-one");

        Assert.True(result.IsAuthoritative);
        Assert.Equal(0, result.PendingCount);
        Assert.Single(handler.RequestUris);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsUnknown_WhenApiIsUnavailable()
    {
        var handler = new SequencedHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = new MemoryConfirmationApiClient(new HttpClient(handler)
                                                      {
                                                          BaseAddress = new Uri("http://localhost/")
                                                      });

        var result = await client.RefreshAsync("conversation-one");

        Assert.False(result.IsAuthoritative);
        Assert.Equal(0, result.PendingCount);
    }

    private sealed class SequencedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public List<Uri> RequestUris { get; } = new();

        public SequencedHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
