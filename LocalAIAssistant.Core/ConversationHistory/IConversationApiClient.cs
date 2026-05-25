namespace LocalAIAssistant.Core.ConversationHistory;

public interface IConversationApiClient
{
    Task<IReadOnlyList<ConversationSummaryDto>> GetAllConversationsAsync (CancellationToken ct = default);
    Task                                        RenameConversationAsync  (string conversationId, string name, CancellationToken ct = default);
    Task<bool>                                  DeleteConversationAsync  (string conversationId, CancellationToken ct = default);
}
