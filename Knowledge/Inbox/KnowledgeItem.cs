namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class KnowledgeItem
{
    public Guid   Id       { get; init; }
    public string IdString => Id.ToString(); // convenience for SQLite store

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
    public bool IsQueued { get; init; }  // true = came from offline queue, not yet API-processed
}

public enum KnowledgeKind
{
    Journal
  , Task
  , Pending // queued offline — kind not yet assigned by API
}

public enum KnowledgeStatus
{
    Active
  , Completed
  , Archived
  , Deleted
}
