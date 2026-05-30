using System.Net.Http.Json;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Notifications;

public class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;

    public NotificationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NotificationSchedule> GetNotificationScheduleAsync(DateTimeOffset         from
                                                                        , CancellationToken ct = default)
    {
        try
        {
            var encoded  = Uri.EscapeDataString(from.ToString("O"));
            var response = await _httpClient.GetAsync($"api/notifications/schedule?from={encoded}", ct);

            response.EnsureSuccessStatusCode();

            return await response.Content
                                 .ReadFromJsonAsync<NotificationSchedule>(cancellationToken: ct)
                   ?? new NotificationSchedule();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return new NotificationSchedule();
        }
    }
}
