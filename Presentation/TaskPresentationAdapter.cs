using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.Presentation;

/// <summary>
/// Projects an existing task DTO into a read-only presentation model.
/// </summary>
public static class TaskPresentationAdapter
{
    public static PresentationModel Create(TasksDto item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new PresentationModel(
            PresentationStyle.Card,
            item.ShortDescription,
            string.IsNullOrWhiteSpace(item.Details) ? null : item.Details,
            new[]
            {
                new PresentationBadge("Priority", item.Priority.ToString()),
                new PresentationBadge("Status", item.IsCompleted ? "Completed" : "Open")
            },
            item.UpdatedAt == default ? null : item.UpdatedAt);
    }
}
