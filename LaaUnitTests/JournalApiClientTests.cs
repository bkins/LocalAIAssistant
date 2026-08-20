using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LaaUnitTests;

public class JournalApiClientTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsJournalEntry_WhenFound()
    {
        var entry = new JournalEntryDto
        {
            Id        = Guid.NewGuid()
          , CreatedAt = DateTimeOffset.UtcNow
          , Text      = "Reflections on today's engineering goals"
          , Mood      = "Optimistic"
          , MoodScore = 4
        };
        var json = JsonSerializer.Serialize(entry);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetByIdAsync(entry.Id);

        Assert.NotNull(result);
        Assert.Equal(entry.Id, result!.Id);
        Assert.Equal("Reflections on today's engineering goals", result.Text);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDtoWithError_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.NotNull(result!.Error);
        Assert.Contains("Simulated network failure", result.Error.Message);
    }

    [Fact]
    public async Task GetRevisionsAsync_ReturnsList_WhenFound()
    {
        var revisions = new List<JournalRevisionDto>
        {
            new() { RevisionId = Guid.NewGuid(), Text = "Initial draft" },
            new() { RevisionId = Guid.NewGuid(), Text = "Revised draft" }
        };
        var json = JsonSerializer.Serialize(revisions);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetRevisionsAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public async Task GetRevisionsAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.GetRevisionsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMostRecentAsync_ReturnsEntry_WhenFound()
    {
        var entry = new JournalEntryDto
        {
            Id   = Guid.NewGuid()
          , Text = "Most recent note"
        };
        var json = JsonSerializer.Serialize(entry);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetMostRecentAsync();

        Assert.NotNull(result);
        Assert.Equal(entry.Id, result!.Id);
    }

    [Fact]
    public async Task EditEntryAsync_CompletesSuccessfully_WhenOk()
    {
        var entryId = Guid.NewGuid();
        var sut     = BuildClient(HttpStatusCode.OK, "{}");

        var exception = await Record.ExceptionAsync(() => sut.EditEntryAsync(
            entryId,
            "Updated text",
            ["work", "focus"],
            "Calm",
            3));

        Assert.Null(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JournalApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new JournalApiClient(client);
    }

    private static JournalApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
        return new JournalApiClient(client);
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

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Simulated network failure");
    }
}
