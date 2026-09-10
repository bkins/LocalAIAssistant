using LocalAIAssistant.Knowledge.Inbox;

namespace LocalAIAssistant.Presentation;

/// <summary>
/// Projects existing inbox data into a display model without changing its source or lifecycle.
/// </summary>
public static class KnowledgeItemPresentationAdapter
{
    public static PresentationModel Create(KnowledgeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new PresentationModel(
            PresentationStyle.Card,
            item.Title,
            string.IsNullOrWhiteSpace(item.Summary) ? null : item.Summary,
            new[]
            {
                new PresentationBadge("Kind", item.Kind.ToString()),
                new PresentationBadge("Status", ToStatusLabel(item.Status))
            },
            item.LastModifiedAt == default ? null : item.LastModifiedAt);
    }

    private static string ToStatusLabel(KnowledgeStatus status) => status switch
    {
        KnowledgeStatus.Active => "Active",
        KnowledgeStatus.Completed => "Completed",
        KnowledgeStatus.Archived => "Archived",
        KnowledgeStatus.Deleted => "Deleted",
        _ => "Unknown"
    };
}
