using System.Runtime.InteropServices;
using LocalAIAssistant.Core.Notifications;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace LocalAIAssistant.Services;

public sealed class PluginLocalNotificationScheduler : INotificationScheduler
{
    private readonly INotificationService _notificationService;
    private readonly ILoggingService      _loggingService;

    public PluginLocalNotificationScheduler(INotificationService notificationService
                                          , ILoggingService      loggingService)
    {
        _notificationService = notificationService;
        _loggingService      = loggingService;
    }

    public Task<bool> AreNotificationsEnabledAsync()
        => _notificationService.AreNotificationsEnabled(new NotificationPermission());

    public void CancelAll()
        => _notificationService.CancelAll();

    public async Task ScheduleAsync(int      notificationId
                                  , string   title
                                  , string   description
                                  , DateTime notifyTime
                                  , string   channelId)
    {
        try
        {
            await _notificationService.Show(new NotificationRequest
                  {
                      NotificationId = notificationId
                    , Title          = title
                    , Description    = description
                    , Schedule       = new NotificationRequestSchedule { NotifyTime = notifyTime }
                    , Android        = new AndroidOptions { ChannelId = channelId }
                  });
        }
        catch (COMException ex)
        {
            // Windows: AppNotificationManager requires packaged-app registration or COM activator.
            // Log a warning and continue — individual notification failures must not abort the sync loop.
            _loggingService.LogWarning($"Windows notification skipped (COMException 0x{ex.HResult:X8}): {ex.Message}"
                                     , Category.App);
        }
    }
}
