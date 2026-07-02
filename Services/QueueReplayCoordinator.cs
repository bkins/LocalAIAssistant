using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.Services.Interfaces;
using System.Threading.Tasks;
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
                Console.WriteLine($"Queue replay failed: {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        });
    }
}