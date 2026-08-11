using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Core.ConversationHistory;

namespace LocalAIAssistant.Services;

public class AgentJobService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentJobService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.CpApi);
        _jsonOptions = new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       };
    }

    public async Task<AgentJobDto> CreateJobAsync(string prompt, string? conversationId)
    {
        var payload = new { Prompt = prompt, ConversationId = conversationId };
        var response = await _httpClient.PostAsJsonAsync("api/agent/jobs", payload);
        response.EnsureSuccessStatusCode();

        var job = await response.Content.ReadFromJsonAsync<AgentJobDto>(_jsonOptions);
        return job ?? throw new InvalidOperationException("Failed to deserialize created agent job.");
    }

    public async Task<AgentJobDto?> GetJobAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/agent/jobs/{jobId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AgentJobDto>(_jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ConversationMetadataDto>> ListConversationsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/conversation");
            response.EnsureSuccessStatusCode();

            var list = await response.Content.ReadFromJsonAsync<List<ConversationMetadataDto>>(_jsonOptions);
            return list ?? new List<ConversationMetadataDto>();
        }
        catch
        {
            return new List<ConversationMetadataDto>();
        }
    }

    public async Task<List<ConversationTurnDto>> GetHistoryAsync(string conversationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/conversation/{conversationId}/history");
            response.EnsureSuccessStatusCode();

            var list = await response.Content.ReadFromJsonAsync<List<ConversationTurnDto>>(_jsonOptions);
            return list ?? new List<ConversationTurnDto>();
        }
        catch
        {
            return new List<ConversationTurnDto>();
        }
    }
}
