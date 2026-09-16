using LocalAIAssistant.Knowledge.Inbox;

namespace LaaUnitTests;

public class KnowledgeItemDetailNavigationTests
{
    [Theory]
    [InlineData(KnowledgeKind.Journal, "JournalDetailPage")]
    [InlineData(KnowledgeKind.Task, "TaskDetailPage")]
    public async Task OpenAsync_SupportedKind_PreservesRouteAndEscapedWorkspace(KnowledgeKind kind, string page)
    {
        var item = new KnowledgeItem { Id = Guid.NewGuid(), Kind = kind, Workspace = "Work & Home" };
        string? route = null;

        var error = await KnowledgeItemDetailNavigation.OpenAsync(item, url => { route = url; return Task.CompletedTask; });

        Assert.Null(error);
        Assert.Equal($"{page}?id={item.Id}&workspace=Work%20%26%20Home", route);
    }

    [Theory]
    [InlineData(KnowledgeKind.Meal)]
    [InlineData(KnowledgeKind.Conversation)]
    [InlineData((KnowledgeKind)999)]
    public async Task OpenAsync_UnsupportedKind_ReturnsFeedbackWithoutNavigating(KnowledgeKind kind)
    {
        var item = new KnowledgeItem { Kind = kind };
        var navigated = false;

        var error = await KnowledgeItemDetailNavigation.OpenAsync(item, _ => { navigated = true; return Task.CompletedTask; });

        Assert.NotNull(error);
        Assert.Contains("not supported", error);
        Assert.False(navigated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenAsync_NavigationThrows_ReturnsNonfatalFeedback(bool asynchronous)
    {
        var item = new KnowledgeItem { Kind = KnowledgeKind.Journal };

        var error = await KnowledgeItemDetailNavigation.OpenAsync(item, _ => asynchronous
            ? Task.FromException(new InvalidOperationException("Navigation failed"))
            : throw new InvalidOperationException("Navigation failed"));

        Assert.NotNull(error);
        Assert.Contains("Unable to open", error);
    }

    [Theory]
    [InlineData(KnowledgeKind.Pending, false)]
    [InlineData(KnowledgeKind.Journal, true)]
    public async Task OpenAsync_PendingOrQueued_DoesNotNavigate(KnowledgeKind kind, bool queued)
    {
        var item = new KnowledgeItem { Kind = kind, IsQueued = queued };
        var navigated = false;

        var error = await KnowledgeItemDetailNavigation.OpenAsync(item, _ => { navigated = true; return Task.CompletedTask; });

        Assert.Null(error);
        Assert.False(navigated);
    }
}
