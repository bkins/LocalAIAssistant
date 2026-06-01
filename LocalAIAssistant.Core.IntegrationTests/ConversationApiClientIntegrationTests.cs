using LocalAIAssistant.Core.ConversationHistory;

namespace LocalAIAssistant.LocalAIAssistant.Core.IntegrationTests;

/// <summary>
/// Contract tests for <see cref="ConversationApiClient"/> against the live CP dev API.
/// Prerequisite: CognitivePlatform.Api running at http://localhost:5273
/// </summary>
[Trait("Category", "Integration")]
public class ConversationApiClientIntegrationTests
{
    private const string BaseUrl = "http://localhost:5273";

    private readonly ITestOutputHelper     _output;
    private readonly ConversationApiClient _client;

    public ConversationApiClientIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _client = new ConversationApiClient(
                      new HttpClient { BaseAddress = new Uri(BaseUrl) });
    }

    // ── GetAllConversationsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllConversationsAsync_ReturnsNonNull_WhenApiIsReachable()
    {
        var result = await _client.GetAllConversationsAsync();

        _output.WriteLine($"Conversations returned: {result.Count}");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllConversationsAsync_EachItem_HasNonEmptyConversationId()
    {
        var result = await _client.GetAllConversationsAsync();

        foreach (var conversation in result)
        {
            _output.WriteLine($"  id={conversation.ConversationId}  "
                            + $"name={conversation.Name ?? "<null>"}  "
                            + $"messages={conversation.MessageCount}  "
                            + $"lastActive={conversation.LastActiveUtc:O}");

            conversation.ConversationId.Should().NotBeNullOrEmpty(
                "ConversationSummaryDto.ConversationId is the primary key — server must return it");
        }
    }

    [Fact]
    public async Task GetAllConversationsAsync_EachItem_HasNonNegativeMessageCount()
    {
        var result = await _client.GetAllConversationsAsync();

        foreach (var conversation in result)
        {
            conversation.MessageCount.Should().BeGreaterThanOrEqualTo(0,
                "message count reflects stored turns — must never be negative");
        }
    }

    [Fact]
    public async Task GetAllConversationsAsync_EachItem_HasLastActiveUtcAfter2024()
    {
        var lowerBound = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result     = await _client.GetAllConversationsAsync();

        foreach (var conversation in result)
        {
            conversation.LastActiveUtc.Should().BeAfter(lowerBound,
                "deserialized LastActiveUtc must round-trip as a valid, recent UTC timestamp");
        }
    }

    // ── DeleteConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversationAsync_ReturnsFalse_WhenConversationIdDoesNotExist()
    {
        var result = await _client.DeleteConversationAsync("integration-test-nonexistent-id");

        _output.WriteLine($"Delete result for non-existent id: {result}");

        result.Should().BeFalse("the server must return a non-success status for an unknown id");
    }

    // ── RenameConversationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RenameConversationAsync_DoesNotThrow_WhenApiIsReachable()
    {
        var conversations = await _client.GetAllConversationsAsync();

        if (!conversations.Any())
        {
            _output.WriteLine("Skipping: no conversations exist in the dev API — nothing to rename.");
            return;
        }

        var target    = conversations[0];
        var exception = await Record.ExceptionAsync(
            () => _client.RenameConversationAsync(target.ConversationId
                                                , target.Name ?? "Integration-Test-Rename"));

        _output.WriteLine($"Rename target: {target.ConversationId}  exception: {exception?.GetType().Name ?? "none"}");

        exception.Should().BeNull("RenameConversationAsync swallows errors by contract — must not surface them");
    }
}
