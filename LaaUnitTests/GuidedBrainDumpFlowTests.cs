using LocalAIAssistant.Core.BrainDump;
using Moq;

namespace LaaUnitTests;

public class GuidedBrainDumpFlowTests
{
    private readonly Mock<IBrainDumpApiClient> _clientMock = new();
    private readonly GuidedBrainDumpFlow       _flow;

    private static readonly Func<string, CancellationToken, Task<string>> NoOpConverseFn
        = (_, _) => Task.FromResult(string.Empty);

    public GuidedBrainDumpFlowTests()
    {
        _clientMock.Setup(client => client.StartSessionAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BrainDumpSessionDto { Id = "test-session" });

        _clientMock.Setup(client => client.UpdateCategoryAsync( It.IsAny<string>()
                                                              , It.IsAny<BrainDumpCategoryField>()
                                                              , It.IsAny<string>()
                                                              , It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BrainDumpSessionDto { Id = "test-session" });

        _clientMock.Setup(client => client.GetSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BrainDumpSessionDto { Id = "test-session" });

        _clientMock.Setup(client => client.MarkProcessedAsync( It.IsAny<string>()
                                                             , It.IsAny<string?>()
                                                             , It.IsAny<IReadOnlyList<string>>()
                                                             , It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BrainDumpSessionDto { Id = "test-session", Processed = true });

        _flow = new GuidedBrainDumpFlow(_clientMock.Object);
    }

    // ── IsTrigger ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("brain dump")]
    [InlineData("Brain Dump")]
    [InlineData("BRAIN DUMP")]
    [InlineData("braindump")]
    [InlineData("guided brain dump")]
    [InlineData("start a brain dump")]
    [InlineData("start brain dump")]
    [InlineData("help me unload my thoughts")]
    public void IsTrigger_ReturnsTrue_ForKnownPhrases(string input)
    {
        Assert.True(_flow.IsTrigger(input));
    }

    [Theory]
    [InlineData("show my brain dump")]
    [InlineData("list brain dumps")]
    [InlineData("hello")]
    [InlineData("add task: something")]
    [InlineData("I did a brain dump last week")]
    public void IsTrigger_ReturnsFalse_ForNonTriggers(string input)
    {
        Assert.False(_flow.IsTrigger(input));
    }

    // ── IsActive ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_ReturnsFalse_BeforeStart()
    {
        Assert.False(_flow.IsActive);
    }

    [Fact]
    public async Task IsActive_ReturnsTrue_AfterStart()
    {
        await _flow.StartAsync(NoOpConverseFn);

        Assert.True(_flow.IsActive);
    }

    [Fact]
    public void IsActive_ReturnsFalse_AfterReset()
    {
        _flow.Reset();

        Assert.False(_flow.IsActive);
    }

    // ── StartAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_CreatesSession()
    {
        await _flow.StartAsync(NoOpConverseFn);

        _clientMock.Verify(client => client.StartSessionAsync(It.IsAny<CancellationToken>())
                         , Times.Once);
    }

    [Fact]
    public async Task StartAsync_ReturnsFirstCategoryPrompt()
    {
        var turn = await _flow.StartAsync(NoOpConverseFn);

        Assert.Contains("1 of 7", turn.Message);
        Assert.Contains("Putting Off", turn.Message);
    }

    [Fact]
    public async Task StartAsync_ReturnsActionContinue()
    {
        var turn = await _flow.StartAsync(NoOpConverseFn);

        Assert.Equal(FlowAction.Continue, turn.Action);
    }

    // ── HandleInputAsync — category collection ────────────────────────────────

    [Fact]
    public async Task HandleInputAsync_PatchesSession_WhenAnswerProvided()
    {
        await _flow.StartAsync(NoOpConverseFn);

        await _flow.HandleInputAsync("dentist appointment");

        _clientMock.Verify(client => client.UpdateCategoryAsync( "test-session"
                                                               , BrainDumpCategoryField.Avoidance
                                                               , "dentist appointment"
                                                               , It.IsAny<CancellationToken>())
                         , Times.Once);
    }

    [Fact]
    public async Task HandleInputAsync_DoesNotPatch_WhenSkipped()
    {
        await _flow.StartAsync(NoOpConverseFn);

        await _flow.HandleInputAsync("skip");

        _clientMock.Verify(client => client.UpdateCategoryAsync( It.IsAny<string>()
                                                               , It.IsAny<BrainDumpCategoryField>()
                                                               , It.IsAny<string>()
                                                               , It.IsAny<CancellationToken>())
                         , Times.Never);
    }

    [Fact]
    public async Task HandleInputAsync_ReturnsNextCategoryPrompt_AfterFirstAnswer()
    {
        await _flow.StartAsync(NoOpConverseFn);

        var turn = await _flow.HandleInputAsync("some avoidance text");

        Assert.Contains("2 of 7", turn.Message);
        Assert.Contains("Fears", turn.Message);
    }

    [Fact]
    public async Task HandleInputAsync_IncludesSkippedNote_WhenCategorySkipped()
    {
        await _flow.StartAsync(NoOpConverseFn);

        var turn = await _flow.HandleInputAsync("skip");

        Assert.Contains("skipped", turn.Message);
    }

    // ── HandleInputAsync — done command ───────────────────────────────────────

    [Fact]
    public async Task HandleInputAsync_RunsExtraction_WhenDoneCommandReceived()
    {
        var converseCalled = false;
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) =>
            {
                converseCalled = true;
                return Task.FromResult("TASK: schedule dentist");
            };

        await _flow.StartAsync(converseFn);

        await _flow.HandleInputAsync("done");

        Assert.True(converseCalled);
    }

    // ── HandleInputAsync — all 7 categories ───────────────────────────────────

    [Fact]
    public async Task HandleInputAsync_RunsExtractionAfterAllSevenCategories()
    {
        var converseCalled = false;
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) =>
            {
                converseCalled = true;
                return Task.FromResult(string.Empty);
            };

        await _flow.StartAsync(converseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        Assert.True(converseCalled);
    }

    [Fact]
    public async Task HandleInputAsync_MarksSessionProcessed_WhenNoItemsExtracted()
    {
        await _flow.StartAsync(NoOpConverseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        _clientMock.Verify(client => client.MarkProcessedAsync( "test-session"
                                                              , It.IsAny<string?>()
                                                              , It.IsAny<IReadOnlyList<string>>()
                                                              , It.IsAny<CancellationToken>())
                         , Times.Once);
    }

    [Fact]
    public async Task HandleInputAsync_ReturnsDone_WhenNoItemsExtracted()
    {
        await _flow.StartAsync(NoOpConverseFn);

        FlowTurn lastTurn = null!;
        for (var i = 0; i < 7; i++)
            lastTurn = await _flow.HandleInputAsync("skip");

        Assert.Equal(FlowAction.Done, lastTurn.Action);
    }

    // ── HandleInputAsync — confirmation loop ──────────────────────────────────

    [Fact]
    public async Task HandleInputAsync_EntersConfirmationPhase_WhenItemsExtracted()
    {
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) => Task.FromResult("TASK: schedule dentist\nCONCERN: work-life balance");

        await _flow.StartAsync(converseFn);

        // Complete all categories to trigger extraction
        FlowTurn extractionTurn = null!;
        for (var i = 0; i < 7; i++)
            extractionTurn = await _flow.HandleInputAsync("skip");

        Assert.Contains("1 of 2", extractionTurn.Message);
        Assert.Contains("schedule dentist", extractionTurn.Message);
    }

    [Fact]
    public async Task HandleInputAsync_ReturnsCreateTask_WhenTaskConfirmed()
    {
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) => Task.FromResult("TASK: schedule dentist");

        await _flow.StartAsync(converseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        // Confirm the task
        var turn = await _flow.HandleInputAsync("yes");

        Assert.Equal(FlowAction.CreateTask, turn.Action);
        Assert.Equal("schedule dentist",   turn.TaskTitle);
    }

    [Fact]
    public async Task HandleInputAsync_SkipsItem_WhenAnswerIsNo()
    {
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) => Task.FromResult("TASK: schedule dentist\nTASK: call mom");

        await _flow.StartAsync(converseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        var turn = await _flow.HandleInputAsync("no");

        // Should show item 2 of 2, not create a task
        Assert.Equal(FlowAction.Continue, turn.Action);
        Assert.Null(turn.TaskTitle);
        Assert.Contains("2 of 2", turn.Message);
    }

    [Fact]
    public async Task HandleInputAsync_EndsFLow_WhenSkipRestReceived()
    {
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) => Task.FromResult("TASK: schedule dentist\nTASK: call mom");

        await _flow.StartAsync(converseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        var turn = await _flow.HandleInputAsync("skip rest");

        Assert.Equal(FlowAction.Done, turn.Action);
        Assert.False(_flow.IsActive);
    }

    [Fact]
    public async Task HandleInputAsync_MarksSessionProcessed_AfterConfirmationComplete()
    {
        Func<string, CancellationToken, Task<string>> converseFn =
            (_, _) => Task.FromResult("TASK: schedule dentist");

        await _flow.StartAsync(converseFn);

        for (var i = 0; i < 7; i++)
            await _flow.HandleInputAsync("skip");

        await _flow.HandleInputAsync("yes");

        _clientMock.Verify(client => client.MarkProcessedAsync( "test-session"
                                                              , It.IsAny<string?>()
                                                              , It.IsAny<IReadOnlyList<string>>()
                                                              , It.IsAny<CancellationToken>())
                         , Times.Once);
    }

    // ── Cancel command ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleInputAsync_ReturnsCancel_WhenCancelReceived()
    {
        await _flow.StartAsync(NoOpConverseFn);

        var turn = await _flow.HandleInputAsync("cancel");

        Assert.Equal(FlowAction.Done, turn.Action);
        Assert.False(_flow.IsActive);
    }

    // ── ParseExtractedItems (internal logic tested via reflection-level access) ─

    [Theory]
    [InlineData("TASK: schedule dentist",       ExtractedItemType.Task,    "schedule dentist")]
    [InlineData("task: call mom",               ExtractedItemType.Task,    "call mom")]
    [InlineData("CONCERN: work-life balance",   ExtractedItemType.Concern, "work-life balance")]
    [InlineData("PATTERN: avoidance of conflict", ExtractedItemType.Pattern, "avoidance of conflict")]
    public void ParseExtractedItems_ParsesValidLines(string line, ExtractedItemType expectedType, string expectedDescription)
    {
        var items = GuidedBrainDumpFlow.ParseExtractedItems(line);

        Assert.Single(items);
        Assert.Equal(expectedType,        items[0].Type);
        Assert.Equal(expectedDescription, items[0].Description);
    }

    [Fact]
    public void ParseExtractedItems_IgnoresUnrecognizedLines()
    {
        var response = """
                       Here are your items:
                       TASK: schedule dentist
                       Some filler text
                       CONCERN: work stress
                       """;

        var items = GuidedBrainDumpFlow.ParseExtractedItems(response);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ParseExtractedItems_ReturnsEmpty_WhenNoItemsFound()
    {
        var items = GuidedBrainDumpFlow.ParseExtractedItems("No items here.");

        Assert.Empty(items);
    }

    [Fact]
    public void ParseExtractedItems_ReturnsEmpty_WhenInputIsEmpty()
    {
        var items = GuidedBrainDumpFlow.ParseExtractedItems(string.Empty);

        Assert.Empty(items);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reset_DeactivatesFlow()
    {
        await _flow.StartAsync(NoOpConverseFn);

        _flow.Reset();

        Assert.False(_flow.IsActive);
    }
}
