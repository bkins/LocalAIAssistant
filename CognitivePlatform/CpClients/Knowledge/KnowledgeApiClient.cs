using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public sealed class KnowledgeApiClient : IKnowledgeApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public KnowledgeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/knowledge", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<KnowledgeItem>>(JsonOptions, ct);
        return result ?? (IReadOnlyList<KnowledgeItem>)Array.Empty<KnowledgeItem>();
    }

    public async Task ArchiveAsync (Guid itemId)
    {
        // var request = new HttpRequestMessage(HttpMethod.Post
        //                                    , $"api/knowledge/{itemId}/archive");
        //await _httpClient.po .PostAsync(request);
        
        using var request = new HttpRequestMessage(HttpMethod.Post
                                                 , $"api/knowledge/{itemId}/archive")
                            {
                                    Content = JsonContent.Create(new
                                                                 {
                                                                         itemId
                                                                 })
                            };

        using var response = await _httpClient.SendAsync(request
                                                       , HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
    }

    public async Task<ConverseResponseDto> ArchiveInboxItemToVaultAsync(Guid itemId, string kind)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/secrets/archive-inbox-item")
                            {
                                    Content = JsonContent.Create(new
                                                                 {
                                                                         ItemId = itemId
                                                                       , Kind   = kind
                                                                 })
                            };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConverseResponseDto>()
            ?? new ConverseResponseDto { Message = "Failed to archive item to vault." };
    }
}