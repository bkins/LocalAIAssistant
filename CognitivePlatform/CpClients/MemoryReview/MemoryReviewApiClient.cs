using System.Net.Http.Json;

namespace LocalAIAssistant.CognitivePlatform.CpClients.MemoryReview;

public sealed class MemoryReviewApiClient : IMemoryReviewApiClient
{
    private readonly HttpClient _httpClient;

    public MemoryReviewApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MemoryReviewDto?> GetReviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MemoryReviewDto>("api/memory/review", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
