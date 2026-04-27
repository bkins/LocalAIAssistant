using CognitivePlatform.Api.Data;
using LocalAIAssistant.Core.Data;

namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class SqliteLocalKnowledgeStore : ILocalKnowledgeStore
{
    private readonly SqliteObjectStore _store;

    public SqliteLocalKnowledgeStore(string localDbPath)
    {
        // BACK-08: route via the shared helper instead of interpolating localDbPath
        // into the connection string directly.
        _store = new SqliteObjectStore(SqliteConnectionStrings.ForDataSource(localDbPath));
    }

    public void Save(KnowledgeItem item)
        => _store.Save(item, id: item.Id.ToString());

    public KnowledgeItem? Get(Guid id)
        => _store.Get<KnowledgeItem>(id.ToString());

    public IReadOnlyList<KnowledgeItem> List()
        => _store.List<KnowledgeItem>();

    public void Clear()
    {
        var all = _store.List<KnowledgeItem>();
        foreach (var item in all)
            _store.SoftDelete<KnowledgeItem>(item.Id.ToString());
    }
    
    // TODO: Consider adding a Purge method, instead of using the Clear method since it only SoftDeletes items.
    
}