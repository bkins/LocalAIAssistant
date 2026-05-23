namespace LocalAIAssistant.Core.ConversationHistory;

public interface IConversationHistoryClient
{
    Task<IReadOnlyList<ConversationTurnDto>> GetHistoryAsync( string            conversationId
                                                            , int               last = 20
                                                            , CancellationToken ct   = default );
}
