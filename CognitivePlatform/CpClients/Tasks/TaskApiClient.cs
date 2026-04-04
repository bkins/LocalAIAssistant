using System.Net;
using System.Net.Http.Json;
using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

public class TaskApiClient : ITaskApiClient
{
    private readonly HttpClient _httpClient;

    public TaskApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TasksDto?> GetByIdAsync( Guid              id
                                             , CancellationToken ct = default )
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/tasks/{id}", ct);

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
            return null;
        }
    }

    public async Task<IReadOnlyList<TasksDto>> GetAllAsync( CancellationToken ct = default )
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tasks", ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                                       .ReadFromJsonAsync<List<TasksDto>>(cancellationToken: ct);

            return result ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}