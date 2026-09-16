using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class KnowledgeSyncService : IKnowledgeSyncService
{
    private readonly IKnowledgeClientFactory _clientFactory;
    private readonly ILocalKnowledgeStore    _localStore;
    private readonly IConnectivityReporter    _connectivity;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    public bool IsOnline => _connectivity.Online();

    public KnowledgeSyncService( IKnowledgeClientFactory clientFactory
                               , ILocalKnowledgeStore    localStore
                               , IConnectivityReporter    connectivity )
    {
        _clientFactory = clientFactory;
        _localStore    = localStore;
        _connectivity  = connectivity;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        if (IsOnline.Not()) return;

        await _syncGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var client = _clientFactory.Create();
            var items  = await client.GetKnowledgeAsync(ct).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();

            // Cache APIs are synchronous SQLite operations. Never run the per-row
            // clear/save loop on the page's UI continuation, even for a completed HTTP task.
            await Task.Run(() =>
            {
                _localStore.Clear();
                foreach (var item in items)
                {
                    _localStore.Save(item);
                }
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task<IReadOnlyList<KnowledgeItem>> ReadLocalAsync(CancellationToken ct = default)
    {
        // Readers share the write gate so they cannot observe the cache between
        // Clear and the last Save when another Inbox instance refreshes.
        await _syncGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(_localStore.List, ct).ConfigureAwait(false);
        }
        finally
        {
            _syncGate.Release();
        }
    }
}
