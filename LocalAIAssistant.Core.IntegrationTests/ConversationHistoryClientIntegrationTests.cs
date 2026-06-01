using LocalAIAssistant.Core.ConversationHistory;

namespace LocalAIAssistant.LocalAIAssistant.Core.IntegrationTests;

/// <summary>
/// Contract tests for <see cref="ConversationHistoryClient"/> against the live CP dev API.
/// Prerequisite: CognitivePlatform.Api running at http://localhost:5273
/// </summary>
[Trait("Category", "Integration")]
public class ConversationHistoryClientIntegrationTests
{
    private const string BaseUrl = "http://localhost:5273";

    private readonly ITestOutputHelper        _output;
    private readonly ConversationHistoryClient _historyClient;
    private readonly ConversationApiClient     _conversationClient;

    public ConversationHistoryClientIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        _historyClient      = new ConversationHistoryClient(httpClient);
        _conversationClient = new ConversationApiClient(httpClient);
    }

    // ── GetHistoryAsync — unknown conversation ────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyList_WhenConversationIdIsUnknown()
    {
        var result = await _historyClient.GetHistoryAsync("integration-test-nonexistent-conv-id");

        _output.WriteLine($"Turns returned for unknown id: {result.Count}");

        result.Should().NotBeNull();
        result.Should().BeEmpty("the client swallows errors and returns an empty list for unknown ids");
    }

    // ── GetHistoryAsync — known conversation ──────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_EachTurn_HasNonNullRoleAndContent()
    {
        var conversations = await _conversationClient.GetAllConversationsAsync();

        if (!conversations.Any())
        {
            _output.WriteLine("Skipping: no conversations in dev API.");
            return;
        }

        var target = conversations[0];
        var result = await _historyClient.GetHistoryAsync(target.ConversationId, last: 5);

        _output.WriteLine($"Conversation: {target.ConversationId}  Turns: {result.Count}");

        foreach (var turn in result)
        {
            _output.WriteLine($"  role={turn.Role}  ts={turn.Timestamp:O}  content.len={turn.Content.Length}");

            turn.Role.Should().NotBeNullOrEmpty(
                "ConversationTurnDto.Role must always be set (e.g. 'user' or 'assistant')");
            turn.Content.Should().NotBeNull(
                "Content may be empty but the field itself must deserialize as non-null");
        }
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsAtMostLastNTurns_WhenLastIsSmall()
    {
        var conversations = await _conversationClient.GetAllConversationsAsync();

        if (!conversations.Any())
        {
            _output.WriteLine("Skipping: no conversations in dev API.");
            return;
        }

        var target    = conversations[0];
        const int limit = 3;

        var result = await _historyClient.GetHistoryAsync(target.ConversationId, last: limit);

        _output.WriteLine($"  Requested last={limit}  received={result.Count}");

        result.Count.Should().BeLessThanOrEqualTo(limit,
            "the server must honour the 'last' query parameter as an upper bound");
    }

    [Fact]
    public async Task GetHistoryAsync_EachTurn_HasTimestampAfter2024()
    {
        var conversations = await _conversationClient.GetAllConversationsAsync();

        if (!conversations.Any())
        {
            _output.WriteLine("Skipping: no conversations in dev API.");
            return;
        }

        var target     = conversations[0];
        var result     = await _historyClient.GetHistoryAsync(target.ConversationId, last: 20);
        var lowerBound = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        foreach (var turn in result)
        {
            turn.Timestamp.Should().BeAfter(lowerBound,
                "ConversationTurnDto.Timestamp must deserialize as a valid DateTimeOffset after 2024-01-01");
        }
    }
}
