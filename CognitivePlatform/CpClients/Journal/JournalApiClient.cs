using System.Net;
using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Journal;

public sealed class JournalApiClient : IJournalApiClient
{
    private readonly HttpClient _httpClient;

    public JournalApiClient (HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JournalEntryDto?> GetByIdAsync (Guid              id
                                                    , CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/journals/{id}"
                                                    , ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<JournalEntryDto>(cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            // Offline, DNS failure, server unreachable, etc.
            return null;
        }
    }

    public async Task<IReadOnlyList<JournalRevisionDto>?> GetRevisionsAsync (Guid              journalId
                                                                           , CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/journals/{journalId}/revisions"
                                                    , ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<IReadOnlyList<JournalRevisionDto>>(cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            // Offline, DNS failure, server unreachable, etc.
            return null;
        }
    }
}