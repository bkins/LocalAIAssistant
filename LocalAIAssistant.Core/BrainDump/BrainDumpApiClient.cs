using System.Net;
using System.Net.Http.Json;

namespace LocalAIAssistant.Core.BrainDump;

public class BrainDumpApiClient : IBrainDumpApiClient
{
    private readonly HttpClient _http;

    public BrainDumpApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<BrainDumpSessionDto> StartSessionAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/braindumps", new { }, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content
                             .ReadFromJsonAsync<BrainDumpSessionDto>(cancellationToken: ct)
               ?? new BrainDumpSessionDto();
    }

    public async Task<BrainDumpSessionDto?> GetSessionAsync( string            id
                                                           , CancellationToken ct = default )
    {
        try
        {
            var response = await _http.GetAsync($"api/braindumps/{id}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<BrainDumpSessionDto>(cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }

    public async Task<BrainDumpSessionDto?> UpdateCategoryAsync( string                  id
                                                               , BrainDumpCategoryField  field
                                                               , string                  text
                                                               , CancellationToken       ct = default )
    {
        var body = BuildPatchBody(field, text);

        try
        {
            var response = await _http.PatchAsJsonAsync($"api/braindumps/{id}", body, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<BrainDumpSessionDto>(cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }

    public async Task<BrainDumpSessionDto?> MarkProcessedAsync( string                 id
                                                              , string?                summary
                                                              , IReadOnlyList<string>  taskIds
                                                              , CancellationToken      ct = default )
    {
        var body = new
                   {
                       extractionSummary    = summary
                     , extractedTaskIds     = taskIds
                     , extractedInsightIds  = Array.Empty<string>()
                   };

        try
        {
            var response = await _http.PostAsJsonAsync($"api/braindumps/{id}/process", body, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<BrainDumpSessionDto>(cancellationToken: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException)       { return null; }
    }

    // Builds a patch body where only the target field is non-null so the server
    // leaves all other category fields untouched.
    private static object BuildPatchBody(BrainDumpCategoryField field, string text)
        => field switch
           {
               BrainDumpCategoryField.Avoidance        => new { avoidance        = text }
             , BrainDumpCategoryField.Fears             => new { fears            = text }
             , BrainDumpCategoryField.Frustrations      => new { frustrations     = text }
             , BrainDumpCategoryField.Discouragements   => new { discouragements  = text }
             , BrainDumpCategoryField.GoalsAndBarriers  => new { goalsAndBarriers = text }
             , BrainDumpCategoryField.HurtAndSorrow     => new { hurtAndSorrow    = text }
             , BrainDumpCategoryField.SelfCriticism     => new { selfCriticism    = text }
             , _                                        => throw new ArgumentOutOfRangeException(nameof(field))
           };
}
