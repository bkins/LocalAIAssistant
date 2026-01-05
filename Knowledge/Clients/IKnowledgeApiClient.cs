using LocalAIAssistant.Knowledge.Inbox;

namespace LocalAIAssistant.Knowledge.Clients;

public interface IKnowledgeApiClient
{
    Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default);
    Task                               ArchiveAsync (Guid                  itemId);
}