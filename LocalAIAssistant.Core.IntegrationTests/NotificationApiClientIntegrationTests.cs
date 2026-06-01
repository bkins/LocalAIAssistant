using LocalAIAssistant.Core.Notifications;

namespace LocalAIAssistant.LocalAIAssistant.Core.IntegrationTests;

/// <summary>
/// Contract tests for <see cref="NotificationApiClient"/> against the live CP dev API.
/// Prerequisite: CognitivePlatform.Api running at http://localhost:5273
///
/// Critical invariant under test: NotificationApiClient propagates HttpRequestException
/// so that NotificationSyncService.SyncAsync can catch it and leave the existing OS
/// schedule intact (CancelAll never runs on a failed sync).
/// </summary>
[Trait("Category", "Integration")]
public class NotificationApiClientIntegrationTests
{
    private const string BaseUrl = "http://localhost:5273";

    private readonly ITestOutputHelper     _output;
    private readonly NotificationApiClient _client;

    public NotificationApiClientIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _client = new NotificationApiClient(
                      new HttpClient { BaseAddress = new Uri(BaseUrl) });
    }

    // ── GetNotificationScheduleAsync — happy path ─────────────────────────────

    [Fact]
    public async Task GetNotificationScheduleAsync_ReturnsNonNull_WhenApiIsReachable()
    {
        var result = await _client.GetNotificationScheduleAsync(DateTimeOffset.UtcNow);

        _output.WriteLine($"Notifications in schedule: {result.Notifications.Count}");

        result.Should().NotBeNull();
        result.Notifications.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotificationScheduleAsync_EachNotification_HasNonEmptyExternalIdAndTitle()
    {
        var result = await _client.GetNotificationScheduleAsync(DateTimeOffset.UtcNow);

        foreach (var notification in result.Notifications)
        {
            _output.WriteLine($"  id={notification.ExternalId}  title={notification.Title}  "
                            + $"fireAt={notification.FireAt:O}  category={notification.Category}");

            notification.ExternalId.Should().NotBeNullOrEmpty(
                "ExternalId is the stable scheduling key used to derive the OS notification id");
            notification.Title.Should().NotBeNullOrEmpty(
                "Title is displayed to the user — must never be empty");
            notification.Body.Should().NotBeNull(
                "Body can be empty but the field itself must deserialize as non-null");
        }
    }

    [Fact]
    public async Task GetNotificationScheduleAsync_EachNotification_HasKnownCategory()
    {
        var result      = await _client.GetNotificationScheduleAsync(DateTimeOffset.UtcNow);
        var validValues = Enum.GetValues<NotificationCategory>();

        foreach (var notification in result.Notifications)
        {
            validValues.Should().Contain(notification.Category,
                $"category value {(int)notification.Category} from server must map to a defined NotificationCategory");
        }
    }

    [Fact]
    public async Task GetNotificationScheduleAsync_EachNotification_FireAtIsAfterRequestTime()
    {
        var from   = DateTimeOffset.UtcNow;
        var result = await _client.GetNotificationScheduleAsync(from);

        foreach (var notification in result.Notifications)
        {
            _output.WriteLine($"  fireAt={notification.FireAt:O}");
            notification.FireAt.Should().BeAfter(from.AddMinutes(-1),
                "scheduled notifications must fire in the near future relative to the 'from' query parameter");
        }
    }

    // ── GetNotificationScheduleAsync — propagation invariant ─────────────────

    [Fact]
    public async Task GetNotificationScheduleAsync_Propagates_HttpRequestException_WhenApiIsUnreachable()
    {
        var unreachableClient = new NotificationApiClient(
                                    new HttpClient { BaseAddress = new Uri("http://localhost:19999") });

        var exception = await Record.ExceptionAsync(
            () => unreachableClient.GetNotificationScheduleAsync(DateTimeOffset.UtcNow));

        _output.WriteLine($"Exception: {exception?.GetType().Name ?? "<null>"}  — {exception?.Message}");

        exception.Should().NotBeNull(
            "NotificationApiClient must NOT swallow HttpRequestException — the sync service depends on it propagating");
        exception.Should().BeOfType<HttpRequestException>(
            "the propagated exception must be HttpRequestException so callers can distinguish network errors");
    }
}
