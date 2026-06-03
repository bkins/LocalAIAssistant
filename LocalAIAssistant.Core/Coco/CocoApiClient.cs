using System.IO;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.Coco.Models;

namespace LocalAIAssistant.Core.Coco;

public sealed class CocoApiClient : ICocoApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CocoApiClient(HttpClient http)
    {
        _http = http;
    }

    // ── Ask (non-streaming) ───────────────────────────────────────────────────

    public async Task<string?> AskAsync(string question, CancellationToken ct = default)
    {
        try
        {
            var payload  = new { prompt = question };
            var response = await _http.PostAsJsonAsync("rag/ask", payload, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(body) ? null : body;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // ── Ask (streaming) ───────────────────────────────────────────────────────

    public async IAsyncEnumerable<CocoAskEvent> AskStreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var payload = new { prompt = question, stream = true };
        var json    = JsonSerializer.Serialize(payload);

        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "rag/ask-stream")
                          {
                              Content = new StringContent(json, Encoding.UTF8, "application/json")
                          };
            response = await _http.SendAsync(request
                                           , HttpCompletionOption.ResponseHeadersRead
                                           , ct);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var       reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var eventJson = line["data: ".Length..];
            if (eventJson == "[DONE]") break;

            CocoAskEvent? ev;
            try
            {
                ev = JsonSerializer.Deserialize<CocoAskEvent>(eventJson, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (ev == null || ev.IsHeartbeat) continue;

            yield return ev;

            if (ev.IsComplete) break;
        }
    }

    // ── Index (fire-and-forget) ───────────────────────────────────────────────

    public async Task IndexPathAsync(string path, bool force = false, CancellationToken ct = default)
    {
        try
        {
            var payload  = new { path, force };
            var response = await _http.PostAsJsonAsync("rag/index", payload, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            // Server returned error or was unreachable — caller handles UI feedback.
        }
    }

    // ── Index (streaming) ─────────────────────────────────────────────────────

    public async IAsyncEnumerable<CocoIndexEvent> IndexStreamAsync(
        string path,
        bool   force = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var payload = new { path, force };
        var json    = JsonSerializer.Serialize(payload);

        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "rag/index-stream")
                          {
                              Content = new StringContent(json, Encoding.UTF8, "application/json")
                          };
            response = await _http.SendAsync(request
                                           , HttpCompletionOption.ResponseHeadersRead
                                           , ct);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var       reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var eventJson = line["data: ".Length..];
            if (eventJson == "[DONE]") break;

            CocoIndexEvent? ev;
            try
            {
                ev = JsonSerializer.Deserialize<CocoIndexEvent>(eventJson, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (ev == null) continue;

            yield return ev;

            if (ev.IsCompleted || ev.IsError) break;
        }
    }

    // ── Health ────────────────────────────────────────────────────────────────

    public async Task<CocoHealthData?> GetHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("rag/health", ct);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                                         .ReadFromJsonAsync<CocoHealthResponse>(JsonOptions
                                                                              , ct);
            return envelope?.Data;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    public async Task<CocoStatsData?> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("rag/stats", ct);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                                         .ReadFromJsonAsync<CocoStatsResponse>(JsonOptions
                                                                             , ct);
            return envelope?.Data;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // ── Status (combined health + stats) ──────────────────────────────────────

    public async Task<CocoStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var healthTask = GetHealthAsync(ct);
            var statsTask  = GetStatsAsync(ct);

            await Task.WhenAll(healthTask, statsTask);

            var health = healthTask.Result;
            var stats  = statsTask.Result;

            if (health == null)
            {
                return new CocoStatusResult
                       {
                           IsReachable  = false
                         , ErrorMessage = "Could not reach Coco API"
                       };
            }

            return new CocoStatusResult
                   {
                       IsReachable       = health.ApiAvailable
                     , OllamaAvailable   = health.OllamaAvailable
                     , StorageAvailable  = health.StorageAvailable
                     , IndexingAvailable = health.IndexingAvailable
                     , TotalChunks       = stats?.TotalChunks ?? 0
                     , UniqueFiles       = stats?.UniqueFiles  ?? 0
                     , LastIndexed       = stats?.LastUpdated
                   };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CocoStatusResult
                   {
                       IsReachable  = false
                     , ErrorMessage = ex.Message
                   };
        }
    }

    // ── Models ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>?> GetModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("rag/models", ct);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                                         .ReadFromJsonAsync<CocoModelListResponse>(JsonOptions
                                                                                 , ct);
            return envelope?.Models?
                            .Select(modelInfo => modelInfo.Name)
                            .ToList()
                            .AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // ── Phase 4 stubs — wiring ready for when Coco adds these endpoints ───────

    // COC-013 pending
    public Task<string?> RefactorAsync(string codeSnippet, CancellationToken ct = default)
        => throw new NotImplementedException("POST /rag/refactor is not yet available — COC-013 pending");

    // COC-013 pending
    public Task<string?> GenerateCommitMessageAsync(string diff, CancellationToken ct = default)
        => throw new NotImplementedException("POST /rag/commit-message is not yet available — COC-013 pending");

    // COC-013 pending
    public Task<string?> ExplainAsync(string codeSnippet, CancellationToken ct = default)
        => throw new NotImplementedException("POST /rag/explain is not yet available — COC-013 pending");

    // COC-013 pending
    public Task<IReadOnlyList<string>?> GetIndexedSymbolsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("GET /rag/symbols is not yet available — COC-013 pending");
}
