using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Knowledge.Inbox;

namespace LaaUnitTests;

public class KnowledgeApiClientTests
{
    [Fact]
    public async Task GetKnowledgeAsync_ReturnsItemsList_WhenSuccessful()
    {
        var items = new List<KnowledgeItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Doc 1", Summary = "Summary 1" },
            new() { Id = Guid.NewGuid(), Title = "Doc 2", Summary = "Summary 2" }
        };
        var json = JsonSerializer.Serialize(items);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetKnowledgeAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ArchiveAsync_CompletesSuccessfully_WhenOk()
    {
        var sut       = BuildClient(HttpStatusCode.OK, "{}");
        var exception = await Record.ExceptionAsync(() => sut.ArchiveAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ArchiveInboxItemToVaultAsync_ReturnsResponseDto_WhenSuccessful()
    {
        var expectedResponse = new ConverseResponseDto
        {
            Message = "Archived to vault successfully"
        };
        var json = JsonSerializer.Serialize(expectedResponse);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.ArchiveInboxItemToVaultAsync(Guid.NewGuid(), "SecretNote");

        Assert.NotNull(result);
        Assert.Equal("Archived to vault successfully", result.Message);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static KnowledgeApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new KnowledgeApiClient(client);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
    }
}
