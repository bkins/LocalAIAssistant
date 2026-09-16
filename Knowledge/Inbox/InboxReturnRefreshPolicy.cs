namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class InboxReturnRefreshPolicy
{
    private bool _returningFromDetail;
    private bool _hasLoaded;
    private long _loadedRevision;

    public void BeginDetailNavigation()
    {
        _returningFromDetail = true;
    }

    public void CancelDetailNavigation()
    {
        _returningFromDetail = false;
    }

    public void MarkLoaded(long revision)
    {
        _hasLoaded = true;
        _loadedRevision = revision;
    }

    public bool ShouldLoadOnAppearance(long revision)
    {
        var reuse = _returningFromDetail && _hasLoaded && _loadedRevision == revision;
        _returningFromDetail = false;
        return !reuse;
    }
}
