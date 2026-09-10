using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CP.Shared.Primitives.Avails.Extensions;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Personas;

public sealed class MemoryConfirmationApiClient : IMemoryConfirmationApiClient
{
    private readonly HttpClient _httpClient;

    public MemoryConfirmationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MemoryConfirmationRefreshResult> RefreshAsync( string            conversationId
                                                                    , CancellationToken cancellationToken = default )
    {
        if (conversationId.HasNoValue())
            return new MemoryConfirmationRefreshResult(false, 0);

        try
        {
            var encodedConversationId = Uri.EscapeDataString(conversationId);
            using var activePersonaResponse = await _httpClient.GetAsync($"api/persona/active/{encodedConversationId}", cancellationToken);

            if (activePersonaResponse.StatusCode == HttpStatusCode.NoContent)
                return new MemoryConfirmationRefreshResult(true, 0);

            if (!activePersonaResponse.IsSuccessStatusCode)
                return new MemoryConfirmationRefreshResult(false, 0);

            var personaId = await activePersonaResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken);
            using var pendingResponse = await _httpClient.GetAsync($"api/persona/{personaId}/memory/pending?conversationId={encodedConversationId}", cancellationToken);

            if (!pendingResponse.IsSuccessStatusCode)
                return new MemoryConfirmationRefreshResult(false, 0);

            await using var pendingContent = await pendingResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var pendingDocument = await JsonDocument.ParseAsync(pendingContent, cancellationToken: cancellationToken);
            var pendingCount = pendingDocument.RootElement.ValueKind == JsonValueKind.Array
                ? pendingDocument.RootElement.GetArrayLength()
                : 0;

            return new MemoryConfirmationRefreshResult(true, pendingCount);
        }
        catch (HttpRequestException)
        {
            return new MemoryConfirmationRefreshResult(false, 0);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new MemoryConfirmationRefreshResult(false, 0);
        }
        catch (JsonException)
        {
            return new MemoryConfirmationRefreshResult(false, 0);
        }
    }

    public async Task<PendingMemorySummaryRefreshResult> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _httpClient.GetFromJsonAsync<PendingMemorySummaryDto>("api/persona/memory/pending-summary", cancellationToken);
            return summary is null
                ? new PendingMemorySummaryRefreshResult(false, 0)
                : new PendingMemorySummaryRefreshResult(true, summary.PendingCount);
        }
        catch (HttpRequestException)
        {
            return new PendingMemorySummaryRefreshResult(false, 0);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PendingMemorySummaryRefreshResult(false, 0);
        }
    }
}

public sealed class PendingMemorySummaryDto
{
    public int PendingCount { get; init; }
}
