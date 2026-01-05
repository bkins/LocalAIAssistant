using System.Net;
using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.Knowledge.Journals.Clients;

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
}