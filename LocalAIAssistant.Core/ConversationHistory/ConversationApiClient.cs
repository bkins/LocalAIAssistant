using System.Net.Http.Json;

namespace LocalAIAssistant.Core.ConversationHistory;

public class ConversationApiClient : IConversationApiClient
{
    private readonly HttpClient _httpClient;

    private const string ConversationRoute = "api/conversation";

    public ConversationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetAllConversationsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ConversationSummaryDto>>(ConversationRoute, ct)
                ?? new List<ConversationSummaryDto>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new List<ConversationSummaryDto>();
        }
    }

    public async Task RenameConversationAsync(string conversationId, string name, CancellationToken ct = default)
    {
        try
        {
            await _httpClient.PutAsJsonAsync($"{ConversationRoute}/{conversationId}/name"
                                           , new { name }
                                           , ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Swallow — UI does not need to know about rename failures in the background
        }
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ConversationRoute}/{conversationId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }
}
