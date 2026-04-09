using System.Text.RegularExpressions;

namespace LaaUnitTests;

// ─────────────────────────────────────────────────────────────────────────────
// Specification tests for TaskListParser
//
// Production source:
//   LocalAIAssistant/CognitivePlatform/Rendering/Parsing/TaskListParser.cs
//
// The UnitTestsFrontend project targets net9.0 and cannot directly reference the
// MAUI project (which requires a MAUI-specific TFM). These tests therefore carry a
// local mirror of the pure parsing logic so the contract can be exercised and
// continuously verified in CI.
//
// BACKLOG-07: when TaskListParser is extracted to a shared net9.0 library, remove
// the local mirror below and replace with a direct using reference.
// ─────────────────────────────────────────────────────────────────────────────

public class TaskListParserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (bool success, List<LocalParsedTask> tasks) TryParse(string text)
    {
        var success = LocalTaskListParser.TryParseTasks(text, out var tasks);
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

// ─────────────────────────────────────────────────────────────────────────────
// Local mirror of production TaskListParser
// Keep in sync with:
//   LocalAIAssistant/CognitivePlatform/Rendering/Parsing/TaskListParser.cs
// ─────────────────────────────────────────────────────────────────────────────

internal static class LocalTaskListParser
{
    private static readonly Regex TaskLineRegex = new(
        @"^- (?<id>[a-f0-9]+): (?<title>.+?) \[(?<status>[^]]+)\](?: \[tags: (?<tags>[^]]+)\])?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParseTasks(string text, out List<LocalParsedTask> tasks)
    {
        tasks = new List<LocalParsedTask>();

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var m = TaskLineRegex.Match(line.Trim());
            if (!m.Success)
                continue;

            var id     = m.Groups["id"].Value;
            var title  = m.Groups["title"].Value;
            var status = m.Groups["status"].Value;
            var tags   = m.Groups["tags"].Success
                                 ? m.Groups["tags"].Value.Split(',', StringSplitOptions.TrimEntries)
                                 : Array.Empty<string>();

            var importance = status.Contains("Important", StringComparison.OrdinalIgnoreCase);
            var urgent     = status.Contains("Urgent",    StringComparison.OrdinalIgnoreCase);

            tasks.Add(new LocalParsedTask
                      {
                              Id          = id,
                              Title       = title,
                              IsImportant = importance,
                              IsUrgent    = urgent,
                              Tags        = tags.ToList()
                      });
        }

        return tasks.Count > 0;
    }
}

internal class LocalParsedTask
{
    public string       Id          { get; set; } = "";
    public string       Title       { get; set; } = "";
    public bool         IsImportant { get; set; }
    public bool         IsUrgent    { get; set; }
    public List<string> Tags        { get; set; } = new();
}
