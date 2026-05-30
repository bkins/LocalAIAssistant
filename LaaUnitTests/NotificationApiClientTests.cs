using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.Notifications;

namespace LaaUnitTests;

/// <summary>
/// Tests for <see cref="NotificationApiClient"/>.
///
/// Note: <c>NotificationSyncService</c> lives in the MAUI multi-TFM project and cannot be
/// directly referenced from this net9.0 test project. The service-level guarantees are
/// documented in the XML doc on <c>NotificationSyncService.SyncAsync</c>:
///   - API unreachable → exception propagates out of the client (verified here), is caught by
///     SyncAsync's outer catch, and the existing OS schedule is preserved.
///   - Calling SyncAsync twice is idempotent: CancelAll() runs before every reschedule and
///     each notification ID is derived deterministically from ExternalId via a stable hash.
/// </summary>
public class NotificationApiClientTests
{
    // ── GetNotificationScheduleAsync — happy path ─────────────────────────────

    [Fact]
    public async Task GetNotificationScheduleAsync_ReturnsMappedSchedule_WhenApiResponds()
    {
        var fireAt = DateTimeOffset.UtcNow.AddHours(8);
        var json   = JsonSerializer.Serialize(new
        {
            notifications = new[]
            {
                new
                {
                    externalId = "day-open-2026-05-29"
                  , title      = "Good morning"
                  , body       = "Open your day"
                  , fireAt
                  , category   = 1  // DayOpen
                }
            }
        });

        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.GetNotificationScheduleAsync(DateTimeOffset.Now);

        Assert.Single(result.Notifications);
        Assert.Equal("day-open-2026-05-29", result.Notifications[0].ExternalId);
        Assert.Equal("Good morning",        result.Notifications[0].Title);
        Assert.Equal("Open your day",       result.Notifications[0].Body);
        Assert.Equal(NotificationCategory.DayOpen, result.Notifications[0].Category);
    }

    [Fact]
    public async Task GetNotificationScheduleAsync_ReturnsEmptySchedule_WhenResponseBodyIsNull()
    {
        var sut = BuildClient(HttpStatusCode.OK, "null");

        var result = await sut.GetNotificationScheduleAsync(DateTimeOffset.Now);

        Assert.NotNull(result);
        Assert.Empty(result.Notifications);
    }

    // ── GetNotificationScheduleAsync — API-unreachable path ──────────────────
    //
    // These tests verify the critical invariant: HttpRequestException is NOT swallowed
    // by NotificationApiClient. It must propagate up to NotificationSyncService.SyncAsync,
    // whose outer catch block handles it by logging and returning — leaving the existing
    // OS notification schedule intact (CancelAll is never reached).

    [Fact]
    public async Task GetNotificationScheduleAsync_Throws_WhenApiIsUnreachable()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };

        var sut       = new NotificationApiClient(client);
        var exception = await Record.ExceptionAsync(
            () => sut.GetNotificationScheduleAsync(DateTimeOffset.Now));

        Assert.NotNull(exception);
        Assert.IsType<HttpRequestException>(exception);
    }

    [Fact]
    public async Task GetNotificationScheduleAsync_Throws_WhenApiReturnsNonSuccess()
    {
        var sut       = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);
        var exception = await Record.ExceptionAsync(
            () => sut.GetNotificationScheduleAsync(DateTimeOffset.Now));

        Assert.NotNull(exception);
        Assert.IsType<HttpRequestException>(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static NotificationApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        return new NotificationApiClient(client);
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage  request
                                                             , CancellationToken   ct)
            => Task.FromResult(new HttpResponseMessage(_status)
                               {
                                   Content = new StringContent(_content
                                                             , Encoding.UTF8
                                                             , "application/json")
                               });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage  request
                                                             , CancellationToken   ct)
            => throw new HttpRequestException("Simulated network failure");
    }
}
