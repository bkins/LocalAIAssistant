using LocalAIAssistant.Core.ConversationHistory;

namespace LaaUnitTests;

public class ChatHistoryIsolationTests
{
    [Fact]
    public void AppWiring_ConversationCallersAndMemory_UseEnvironmentScope()
    {
        foreach (var filename in new[] { "ChatViewModel", "ConversationsViewModel", "AppShellMasterViewModel" })
        {
            var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", filename + ".cs.txt"));
            Assert.DoesNotContain("StringConsts.ActiveConversationIdKey", source);
            Assert.Contains("ChatHistoryScope.ActiveConversationKey(BuildEnvironment.Name)", source);
        }
        var startup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "MauiProgram.cs.txt"));
        Assert.Contains("ChatHistoryScope.MemoryDirectory(appDir, BuildEnvironment.Name)", startup);
        Assert.Contains("Path.Combine(chatMemoryDir, \"Memory.db\")", startup);
        Assert.Contains("Path.Combine(chatMemoryDir, \"ai_memory.jsonl\")", startup);
        Assert.Contains("Path.Combine(chatMemoryDir, \"facts.json\")", startup);
    }

    [Fact]
    public async Task Load_NonemptyServerHistory_IsAuthoritative()
    {
        var result = await ConversationHistoryLoader.LoadAsync<string>(
            () => Task.FromResult<IReadOnlyList<string>>(new[] { "QA server history" }),
            () => throw new Exception("Unexpected local read"),
            _ => throw new Exception("Unexpected fallback"));
        Assert.Equal(new[] { "QA server history" }, result);
    }
    [Fact]
    public void Scope_EnvironmentPathsAndKeys_DoNotShareLegacyOrOtherEnvironment()
    {
        var environments = new[] { "Dev", "QA", "Prod", "Debug" };
        Assert.Equal(4, environments.Select(ChatHistoryScope.ActiveConversationKey).Distinct().Count());
        Assert.Equal(4, environments.Select(environment => ChatHistoryScope.MemoryDirectory("root", environment)).Distinct().Count());
        Assert.Equal(ChatHistoryScope.ActiveConversationKey("DEV"), ChatHistoryScope.ActiveConversationKey("Development"));
        Assert.DoesNotContain("ActiveConversationId", environments.Select(ChatHistoryScope.ActiveConversationKey));
    }

    [Theory]
    [InlineData("../Dev")]
    [InlineData("")]
    [InlineData("unknown")]
    public void Scope_UnknownOrUnsafeEnvironment_FailsClosed(string environment)
        => Assert.Throws<ArgumentException>(() => ChatHistoryScope.MemoryDirectory("root", environment));

    [Fact]
    public async Task Load_EmptyServerHistory_DoesNotReadLocalMemory()
    {
        var localReads = 0;
        var result = await ConversationHistoryLoader.LoadAsync<string>(
            () => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>()),
            () => { localReads++; return Task.FromResult<IReadOnlyList<string>>(new[] { "Dev memory" }); },
            _ => throw new Exception("Unexpected fallback"));

        Assert.Empty(result);
        Assert.Equal(0, localReads);
    }

    [Fact]
    public async Task Load_ServerFailure_UsesLocalMemoryAndReportsFailure()
    {
        Exception? reported = null;
        var result = await ConversationHistoryLoader.LoadAsync<string>(
            () => Task.FromException<IReadOnlyList<string>>(new HttpRequestException("offline")),
            () => Task.FromResult<IReadOnlyList<string>>(new[] { "QA local memory" }),
            exception => reported = exception);

        Assert.Equal(new[] { "QA local memory" }, result);
        Assert.IsType<HttpRequestException>(reported);
    }

    [Fact]
    public async Task Load_Cancellation_DoesNotUseLocalMemory()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => ConversationHistoryLoader.LoadAsync<string>(
            () => Task.FromException<IReadOnlyList<string>>(new OperationCanceledException()),
            () => throw new Exception("Unexpected local read"),
            _ => throw new Exception("Unexpected fallback")));
    }
}
