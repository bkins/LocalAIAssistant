namespace LocalAIAssistant.Knowledge.Inbox;

/// <summary>Process-local invalidation only; the API remains the data authority.</summary>
public sealed class KnowledgeInboxRefreshState
{
    private long _revision;
    private Func<Task>? _refreshAsync;

    public long Revision => Interlocked.Read(ref _revision);

    public void MarkChanged()
    {
        Interlocked.Increment(ref _revision);
    }

    public void RegisterRefresh(Func<Task> refreshAsync)
    {
        ArgumentNullException.ThrowIfNull(refreshAsync);
        Volatile.Write(ref _refreshAsync, refreshAsync);
    }

    public async Task MarkChangedAndRefreshAsync()
    {
        MarkChanged();
        var refreshAsync = Volatile.Read(ref _refreshAsync);
        if (refreshAsync is not null)
            await refreshAsync();
    }
}
