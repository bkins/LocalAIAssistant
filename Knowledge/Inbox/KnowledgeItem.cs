namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class KnowledgeItem
{
    public Guid Id { get; init; }

    public KnowledgeKind   Kind   { get; init; }
    public KnowledgeStatus Status { get; init; }

    public string  Title   { get; init; } = string.Empty;
    public string? Summary { get; init; }

    public DateTimeOffset CreatedAt           { get; init; }
    public DateTime       CreatedAtLocal      => CreatedAt.ToLocalTime().DateTime;
    public DateTimeOffset LastModifiedAt      { get; init; }
    public DateTime       LastModifiedAtLocal => LastModifiedAt.ToLocalTime().DateTime;

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    
    public bool IsEdited { get; set; }
}

public enum KnowledgeKind
{
    Journal
  , Task
}

public enum KnowledgeStatus
{
    Active
  , Completed
  , Archived
  , Deleted
}
