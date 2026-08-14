using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Inbox;
using LocalAIAssistant.CognitivePlatform.DTOs;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;

public sealed class KnowledgeApiClient : IKnowledgeApiClient
{
    private readonly HttpClient _httpClient;

    public KnowledgeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<KnowledgeItem>> GetKnowledgeAsync(CancellationToken ct = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<KnowledgeItem>>("api/knowledge", ct);

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