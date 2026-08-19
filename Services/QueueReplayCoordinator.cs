using CP.Client.Core.Avails;
using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;

namespace LocalAIAssistant.Services;

public class QueueReplayCoordinator : IDisposable
{
    private readonly IOfflineQueueService _queue;
    private readonly IConnectivityState   _connectivity;
    private readonly ILoggingService      _logger;
    private readonly Timer?               _retryTimer;
    private readonly CancellationTokenSource _cts = new();

    private int  _isProcessing;
    private int  _consecutiveFailures;
    private bool _isDisposed;

    public TimeSpan InitialRetryInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan MaxRetryInterval     { get; set; } = TimeSpan.FromMinutes(5);

    public QueueReplayCoordinator( IOfflineQueueService queue
                                 , IConnectivityState   connectivity
                                 , ILoggingService      logger )
    {
        _queue        = queue;
        _connectivity = connectivity;
        _logger       = logger;

        _connectivity.ConnectivityChanged += OnConnectivityChanged;

        _retryTimer = new Timer(OnTimerTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        if (_connectivity.IsOffline.Not())
        {
            TriggerQueueReplay();
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityStatus connectivityStatus)
    {
        if (_connectivity.IsOffline)
        {
            StopRetryTimer();
            return;
        }

        Interlocked.Exchange(ref _consecutiveFailures, 0);
        TriggerQueueReplay();
    }

    private void OnTimerTick(object? state)
    {
        if (_isDisposed || _connectivity.IsOffline)
        {
            return;
        }

        TriggerQueueReplay();
    }

    private void TriggerQueueReplay()
    {
        if (_isDisposed)
            return;

        // Prevent concurrent processors
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
            return;

        Task.Run(async () =>
        {
            try
            {
                await _queue.ProcessQueueAsync(_cts.Token);
                
                var pendingRemaining = await _queue.GetPendingCountAsync();
                if (pendingRemaining > 0)
                {
                    ScheduleNextRetry();
                }
                else
                {
                    Interlocked.Exchange(ref _consecutiveFailures, 0);
                    StopRetryTimer();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue replay failed", Category.App);
                ScheduleNextRetry();
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        });
    }

    private void ScheduleNextRetry()
    {
        if (_isDisposed || _connectivity.IsOffline)
            return;

        var failures = Interlocked.Increment(ref _consecutiveFailures);
        var multiplier = Math.Min(32, Math.Pow(2, Math.Max(0, failures - 1)));
        var delaySeconds = InitialRetryInterval.TotalSeconds * multiplier;
        var computedInterval = TimeSpan.FromSeconds(Math.Min(delaySeconds, MaxRetryInterval.TotalSeconds));

        try
        {
            _retryTimer?.Change(computedInterval, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // Coordinator is being disposed
        }
    }

    private void StopRetryTimer()
    {
        try
        {
            _retryTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // Coordinator is being disposed
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_connectivity is not null)
        {
            _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        }

        _cts.Cancel();
        _cts.Dispose();
        _retryTimer?.Dispose();
    }
}