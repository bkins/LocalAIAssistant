using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public interface IKnowledgeApiClient
{
    Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default);
    Task                               ArchiveAsync (Guid                  itemId);
    Task<ConverseResponseDto>          ArchiveInboxItemToVaultAsync(Guid itemId, string kind);
}