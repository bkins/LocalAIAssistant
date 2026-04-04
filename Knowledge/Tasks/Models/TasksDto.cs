namespace LocalAIAssistant.Knowledge.Tasks.Models;

/// <summary>
/// Client-side representation of a task returned by the Cognitive Platform API.
/// Mirrors CognitivePlatform.Api.Domains.Tasks.TaskDto.
/// </summary>
public sealed class TasksDto
{
    public string                Id               { get; init; } = string.Empty;
    public string                ShortDescription { get; init; } = string.Empty;
    public string?               Details          { get; init; }
    public TaskPriorityDto       Priority         { get; init; }
    public bool                  IsImportant      { get; init; }
    public bool                  IsUrgent         { get; init; }
    public DateTimeOffset        CreatedAt        { get; init; }
    public DateTimeOffset        UpdatedAt        { get; init; }
    public DateTimeOffset?       DueDate          { get; init; }
    public DateTimeOffset?       CompletedAt      { get; init; }
    public IReadOnlyList<string> Tags             { get; init; } = [];

    public bool IsCompleted => CompletedAt is not null;
}