using LocalAIAssistant.Core.Parsing;
using Xunit;

namespace LaaUnitTests;

// ─────────────────────────────────────────────────────────────────────────────
// Specification tests for TaskListParser
//
// Production source: LocalAIAssistant.Core/Parsing/TaskListParser.cs
// ─────────────────────────────────────────────────────────────────────────────

public class TaskListParserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (bool success, List<ParsedTask> tasks) TryParse(string text)
    {
        var success = TaskListParser.TryParseTasks(text, out var tasks);
        
        return (success, tasks);
    }

    // ── Happy-path parsing ────────────────────────────────────────────────────

    [Fact]
    public void TryParseTasks_ReturnsFalse_WhenInputContainsNoTaskLines()
    {
        var (success, tasks) = TryParse("This is just a regular sentence.\nAnother line.");

        Assert.False(success);
        Assert.Empty(tasks);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleActiveLine_WithNoTags()
    {
        var input = "- abc1: Do the laundry [Active]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.Single(tasks);
        Assert.Equal("abc1",          tasks[0].Id);
        Assert.Equal("Do the laundry", tasks[0].Title);
        Assert.False(tasks[0].IsImportant);
        Assert.False(tasks[0].IsUrgent);
        Assert.Empty(tasks[0].Tags);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleLine_WithImportantStatus()
    {
        var input = "- f1a2b3: Review budget [Important]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.True(tasks[0].IsImportant);
        Assert.False(tasks[0].IsUrgent);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleLine_WithUrgentStatus()
    {
        var input = "- c4d5e6: Fix the server [Urgent]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.False(tasks[0].IsImportant);
        Assert.True(tasks[0].IsUrgent);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleLine_WithBothImportantAndUrgent()
    {
        var input = "- 1a2b3c: Deploy hotfix [Important, Urgent]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.True(tasks[0].IsImportant);
        Assert.True(tasks[0].IsUrgent);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleLine_WithTags()
    {
        var input = "- abc123: Refactor auth [Active] [tags: work, security]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.Equal(2,          tasks[0].Tags.Count);
        Assert.Equal("work",     tasks[0].Tags[0]);
        Assert.Equal("security", tasks[0].Tags[1]);
    }

    [Fact]
    public void TryParseTasks_ParsesSingleLine_WithTagsAndImportantStatus()
    {
        var input = "- dead01: Write tests [Important] [tags: dev, quality]";

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.True(tasks[0].IsImportant);
        Assert.Equal(2,         tasks[0].Tags.Count);
        Assert.Equal("dev",     tasks[0].Tags[0]);
        Assert.Equal("quality", tasks[0].Tags[1]);
    }

    [Fact]
    public void TryParseTasks_ParsesMixedContent_ReturnsOnlyTaskLines()
    {
        var input = """
                    Here are your tasks:
                    - aaa111: First task [Active]
                    Some explanation text.
                    - bbb222: Second task [Urgent] [tags: work]
                    End of list.
                    """;

        var (success, tasks) = TryParse(input);

        Assert.True(success);
        Assert.Equal(2,        tasks.Count);
        Assert.Equal("aaa111", tasks[0].Id);
        Assert.Equal("bbb222", tasks[1].Id);
    }

    [Fact]
    public void TryParseTasks_ParsesMultipleLines_ReturnsAllTasks()
    {
        var input = """
                    - 000001: Task one [Active]
                    - 000002: Task two [Important]
                    - 000003: Task three [Urgent]
                    """;

        var (_, tasks) = TryParse(input);

        Assert.Equal(3, tasks.Count);
        Assert.Equal("Task one",   tasks[0].Title);
        Assert.Equal("Task two",   tasks[1].Title);
        Assert.Equal("Task three", tasks[2].Title);
    }

    [Fact]
    public void TryParseTasks_IsCaseInsensitiveForStatusKeywords()
    {
        var (_, tasks) = TryParse("- abc123: A task [IMPORTANT, URGENT]");

        Assert.True(tasks[0].IsImportant);
        Assert.True(tasks[0].IsUrgent);
    }

    [Fact]
    public void TryParseTasks_ReturnsEmptyTags_WhenNoTagsBlock()
    {
        var (_, tasks) = TryParse("- abc123: A task [Active]");

        Assert.Empty(tasks[0].Tags);
    }
}
