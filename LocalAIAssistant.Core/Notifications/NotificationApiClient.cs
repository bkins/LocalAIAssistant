using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Notifications;

public class NotificationApiClient : INotificationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
                                                                {
                                                                    Converters = { new JsonStringEnumConverter() }
                                                                };

    private readonly HttpClient _httpClient;

    public NotificationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NotificationSchedule> GetNotificationScheduleAsync(DateTimeOffset         from
                                                                        , CancellationToken ct = default)
    {
        var encoded  = Uri.EscapeDataString(from.ToString("O"));
        var response = await _httpClient.GetAsync($"api/notifications/schedule?from={encoded}", ct);

        response.EnsureSuccessStatusCode();

        return await response.Content
                             .ReadFromJsonAsync<NotificationSchedule>(JsonOptions, cancellationToken: ct)
               ?? new NotificationSchedule();
    }
}

