using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.Knowledge.Tasks.Models;
using LocalAIAssistant.Presentation;

namespace LaaUnitTests;

public sealed class PresentationModelTests
{
    [Fact]
    public void KnowledgeInboxMarkup_UsesThePresentationHostWithoutRemovingExistingSwipeActions()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "KnowledgeInboxPage.xaml.txt"));

        Assert.Contains("<controls:PresentationHost", markup);
        Assert.Contains("KnowledgeItemPresentationConverter", markup);
        Assert.Contains("Text=\"Archive\"", markup);
        Assert.Contains("Text=\"Save to Vault\"", markup);
    }

    [Fact]
    public void KnowledgeItemAdapter_ProjectsSemanticFieldsWithoutChangingTheSource()
    {
        var item = new KnowledgeItem
        {
            Title = "Review the quarterly plan",
            Summary = "Prepared after the team check-in.",
            Kind = KnowledgeKind.Task,
            Status = KnowledgeStatus.Active,
            LastModifiedAt = new DateTimeOffset(2026, 9, 9, 15, 30, 0, TimeSpan.Zero)
        };

        var model = KnowledgeItemPresentationAdapter.Create(item);

        Assert.Equal(PresentationStyle.Card, model.Style);
        Assert.Equal(item.Title, model.Title);
        Assert.Equal(item.Summary, model.Summary);
        Assert.Collection(model.Badges,
            badge => Assert.Equal(new PresentationBadge("Kind", "Task"), badge),
            badge => Assert.Equal(new PresentationBadge("Status", "Active"), badge));
        Assert.Equal(item.LastModifiedAt, model.LastModifiedAt);
        Assert.Equal("Prepared after the team check-in.", item.Summary);
    }

    [Fact]
    public void KnowledgeItemAdapter_OmitsBlankSummaryAndUsesUnknownForUnsupportedStatus()
    {
        var item = new KnowledgeItem
        {
            Title = "Untitled note",
            Summary = "   ",
            Kind = KnowledgeKind.Journal,
            Status = (KnowledgeStatus)999
        };

        var model = KnowledgeItemPresentationAdapter.Create(item);

        Assert.Null(model.Summary);
        Assert.Contains(new PresentationBadge("Kind", "Journal"), model.Badges);
        Assert.Contains(new PresentationBadge("Status", "Unknown"), model.Badges);
        Assert.Null(model.LastModifiedAt);
    }

    [Fact]
    public void TaskAdapter_ProjectsExistingTaskFieldsIntoReadOnlyCard()
    {
        var item = new TasksDto
        {
            ShortDescription = "Review execution profile evidence",
            Details          = "Confirm the test evidence before closing the story.",
            Priority         = TaskPriorityDto.High,
            UpdatedAt        = new DateTimeOffset(2026, 9, 11, 15, 30, 0, TimeSpan.Zero)
        };

        var model = TaskPresentationAdapter.Create(item);

        Assert.Equal(PresentationStyle.Card, model.Style);
        Assert.Equal(item.ShortDescription,   model.Title);
        Assert.Equal(item.Details,            model.Summary);
        Assert.Contains(new PresentationBadge("Priority", "High"), model.Badges);
        Assert.Contains(new PresentationBadge("Status", "Open"),    model.Badges);
        Assert.Equal(item.UpdatedAt, model.LastModifiedAt);
    }
}
