using CP.Client.Core.Avails;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Knowledge.Inbox;
using Xunit;

namespace LaaUnitTests;

public class KnowledgeSearchAndHistoryTests
{
    [Fact]
    public void PromptHistory_CyclesBackAndForward_PreservingDraft()
    {
        var history = new List<string>();
        var historyIndex = -1;
        var temporaryDraft = string.Empty;

        void Add(string prompt)
        {
            if (prompt.HasNoValue()) return;
            var trimmed = prompt.Trim();
            if (history.Count == 0 || !history[^1].EqualsIgnoreCase(trimmed))
            {
                history.Add(trimmed);
                if (history.Count > 50) history.RemoveAt(0);
            }
            historyIndex = -1;
            temporaryDraft = string.Empty;
        }

        bool TryRecallPrevious(string currentPrompt, out string prompt)
        {
            if (history.Count == 0)
            {
                prompt = string.Empty;
                return false;
            }

            if (historyIndex == -1)
            {
                temporaryDraft = currentPrompt;
                historyIndex = history.Count - 1;
            }
            else if (historyIndex > 0)
            {
                historyIndex--;
            }

            prompt = history[historyIndex];
            return true;
        }

        bool TryRecallNext(out string prompt)
        {
            if (historyIndex == -1)
            {
                prompt = string.Empty;
                return false;
            }

            if (historyIndex < history.Count - 1)
            {
                historyIndex++;
                prompt = history[historyIndex];
                return true;
            }

            historyIndex = -1;
            prompt = temporaryDraft;
            return true;
        }

        Add("First prompt");
        Add("Second prompt");
        Add("Third prompt");

        var hasPrev1 = TryRecallPrevious("Unsaved draft", out var p1);
        Assert.True(hasPrev1);
        Assert.Equal("Third prompt", p1);

        var hasPrev2 = TryRecallPrevious("Third prompt", out var p2);
        Assert.True(hasPrev2);
        Assert.Equal("Second prompt", p2);

        var hasNext1 = TryRecallNext(out var next1);
        Assert.True(hasNext1);
        Assert.Equal("Third prompt", next1);

        var hasNext2 = TryRecallNext(out var next2);
        Assert.True(hasNext2);
        Assert.Equal("Unsaved draft", next2);
    }

    [Theory]
    [InlineData("/t", "/task")]
    [InlineData("/j", "/journal")]
    [InlineData("/m", "/meal")]
    [InlineData("/h", "/health")]
    [InlineData("/c", "/copilot")]
    [InlineData("id", "Idea:")]
    [InlineData("bu", "Bug:")]
    public void CommandSuggestion_MatchesPrefixQuery(string query, string expectedPrefix)
    {
        var suggestions = new List<CommandSuggestion>
        {
            new() { Prefix = "/task", Title = "/task", CommandTemplate = "Task:" },
            new() { Prefix = "/journal", Title = "/journal", CommandTemplate = "Journal:" },
            new() { Prefix = "/meal", Title = "/meal", CommandTemplate = "/meal" },
            new() { Prefix = "/health", Title = "/health", CommandTemplate = "/health" },
            new() { Prefix = "/copilot", Title = "/copilot", CommandTemplate = "/copilot" },
            new() { Prefix = "Idea:", Title = "Idea:", CommandTemplate = "Idea:" },
            new() { Prefix = "Bug:", Title = "Bug:", CommandTemplate = "Bug:" }
        };

        var matches = suggestions.Where(s => s.Prefix.StartsWithIgnoreCase(query) || s.Title.StartsWithIgnoreCase(query)).ToList();

        Assert.NotEmpty(matches);
        Assert.Contains(matches, s => s.Prefix.EqualsIgnoreCase(expectedPrefix));
    }

    [Fact]
    public void KnowledgeItem_FullTextSearch_FiltersAcrossFields()
    {
        var items = new List<KnowledgeItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Sprint Planning meeting",
                Summary = "Discussed roadmap and milestones",
                Kind = KnowledgeKind.Journal,
                Tags = new[] { "work", "agile" },
                Mood = "Optimistic"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Buy groceries",
                Summary = "Milk, eggs, salmon",
                Kind = KnowledgeKind.Task,
                Tags = new[] { "personal", "shopping" },
                Workspace = "Home"
            }
        };

        bool Matches(KnowledgeItem item, string query)
        {
            return (item.Title.HasValue() && item.Title.ContainsIgnoreCase(query))
                || (item.Summary.HasValue() && item.Summary!.ContainsIgnoreCase(query))
                || (item.Mood.HasValue() && item.Mood!.ContainsIgnoreCase(query))
                || (item.Workspace.HasValue() && item.Workspace!.ContainsIgnoreCase(query))
                || (item.Tags != null && item.Tags.Any(t => t.ContainsIgnoreCase(query)));
        }

        var roadmapMatches = items.Where(i => Matches(i, "roadmap")).ToList();
        Assert.Single(roadmapMatches);
        Assert.Equal("Sprint Planning meeting", roadmapMatches[0].Title);

        var tagMatches = items.Where(i => Matches(i, "shopping")).ToList();
        Assert.Single(tagMatches);
        Assert.Equal("Buy groceries", tagMatches[0].Title);

        var moodMatches = items.Where(i => Matches(i, "optimistic")).ToList();
        Assert.Single(moodMatches);
        Assert.Equal("Sprint Planning meeting", moodMatches[0].Title);

        var wsMatches = items.Where(i => Matches(i, "home")).ToList();
        Assert.Single(wsMatches);
        Assert.Equal("Buy groceries", wsMatches[0].Title);
    }
}
