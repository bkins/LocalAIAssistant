using System.Net;
using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.Knowledge.Tasks.Clients;

public class TaskApiClient : ITaskApiClient
{
    
    private readonly HttpClient _httpClient;

    public TaskApiClient (HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TasksDto?> GetByIdAsync (Guid              id
                                                    , CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/tasks/{id}"
                                                    , ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<TasksDto>(cancellationToken: ct);
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