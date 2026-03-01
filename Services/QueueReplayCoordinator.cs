using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.Services.Interfaces;
using System.Threading;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services;

public class QueueReplayCoordinator
{
    private readonly IOfflineQueueService _queue;
    private readonly IConnectivityState   _connectivity;

    private int _isProcessing = 0;

    public QueueReplayCoordinator( IOfflineQueueService queue,
                                   IConnectivityState   connectivity)
    {
        _queue        = queue;
        _connectivity = connectivity;
        
        if (_connectivity.IsOffline.Not())
        {
            _ = _queue.ProcessQueueAsync();
        }
        
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityStatus connectivityStatus)
    {
        if (_connectivity.IsOffline)
            return;

        // Prevent concurrent processors
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
            return;

        try
        {
            await _queue.ProcessQueueAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Queue replay failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
        }

    }
}