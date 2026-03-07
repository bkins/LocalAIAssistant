namespace LocalAIAssistant.Knowledge.Inbox;

public interface IKnowledgeSyncService
{
    bool IsOnline { get; }
    Task SyncAsync(CancellationToken ct = default);
}