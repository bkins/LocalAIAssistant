using LocalAIAssistant.Knowledge.Inbox;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public interface IKnowledgeApiClient
{
    Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default);
    Task                               ArchiveAsync (Guid                  itemId);
}