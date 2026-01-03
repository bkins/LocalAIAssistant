using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.Knowledge;

public interface IKnowledgeApiClient
{
    Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default);
}