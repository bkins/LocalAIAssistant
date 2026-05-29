using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

namespace LocalAIAssistant.Knowledge.Inbox;

public sealed class KnowledgeSyncService : IKnowledgeSyncService
{
    private readonly IKnowledgeClientFactory _clientFactory;
    private readonly ILocalKnowledgeStore    _localStore;
    private readonly IConnectivityReporter    _connectivity;

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

        var client = _clientFactory.Create();
        var items  = await client.GetKnowledgeAsync(ct);

        ct.ThrowIfCancellationRequested();

        _localStore.Clear();

        foreach (var item in items)
            _localStore.Save(item);
    }
}