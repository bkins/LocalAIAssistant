using System.Net;
using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Journals.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Journal;

public sealed class JournalApiClient : IJournalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string     _journalsApiBaseRoute = "api/journals";

    public JournalApiClient (HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JournalEntryDto?> GetByIdAsync (Guid              id
                                                    , CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_journalsApiBaseRoute}/{id}"
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
            var response = await _httpClient.GetAsync($"{_journalsApiBaseRoute}/{journalId}/revisions"
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

    public async Task EditEntryAsync (Guid                   journalId
                                   , string                 text
                                   , IReadOnlyList<string>? parseTags
                                   , string?                mood
                                   , int?                   moodScore)
    {
        var payload = new
                      {
                              Text      = text
                            , Tags      = parseTags
                            , Mood      = mood
                            , MoodScore = moodScore
                      };

        var response = await _httpClient.PostAsJsonAsync($"{_journalsApiBaseRoute}/{journalId}/edit-test"
                                                       , payload);

        response.EnsureSuccessStatusCode();
    }
}