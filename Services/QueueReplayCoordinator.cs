using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.Services;

public class QueueReplayCoordinator
{
    private readonly IOfflineQueueService _queue;
    private readonly IConnectivityState   _connectivity;
    private readonly ILoggingService      _logger;

    private int _isProcessing = 0;

    public QueueReplayCoordinator( IOfflineQueueService queue
                                 , IConnectivityState   connectivity
                                 , ILoggingService      logger )
    {
        _queue        = queue;
        _connectivity = connectivity;
        _logger       = logger;
        
        _connectivity.ConnectivityChanged += OnConnectivityChanged;

        if (_connectivity.IsOffline.Not())
        {
            TriggerQueueReplay();
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityStatus connectivityStatus)
    {
        if (_connectivity.IsOffline)
            return;

        TriggerQueueReplay();
    }

    private void TriggerQueueReplay()
    {
        // Prevent concurrent processors
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
            return;

        Task.Run(async () =>
        {
            try
            {
                await _queue.ProcessQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue replay failed", Category.App);
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        });
    }
}