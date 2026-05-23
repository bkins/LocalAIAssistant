using System.Net.Http.Json;

namespace LocalAIAssistant.Core.ConversationHistory;

public class ConversationHistoryClient : IConversationHistoryClient
{
    private readonly HttpClient _httpClient;

    private const string HistoryRouteTemplate = "api/conversation/{0}/history";

    public ConversationHistoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ConversationTurnDto>> GetHistoryAsync( string            conversationId
                                                                         , int               last = 20
                                                                         , CancellationToken ct   = default )
    {
        try
        {
            var route = $"{string.Format(HistoryRouteTemplate, conversationId)}?last={last}";

            return await _httpClient.GetFromJsonAsync<List<ConversationTurnDto>>(route, ct)
                ?? new List<ConversationTurnDto>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new List<ConversationTurnDto>();
        }
    }
}
