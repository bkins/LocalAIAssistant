namespace LocalAIAssistant.Knowledge.Inbox;

/// <summary>Process-local invalidation only; the API remains the data authority.</summary>
public sealed class KnowledgeInboxRefreshState
{
    private long _revision;

    public long Revision => Interlocked.Read(ref _revision);

    public void MarkChanged()
    {
        Interlocked.Increment(ref _revision);
    }
}
