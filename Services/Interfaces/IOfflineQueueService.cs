namespace LocalAIAssistant.Services.Interfaces;

public interface IOfflineQueueService
{
    event EventHandler<QueueProcessedEventArgs>? QueueProcessed;
    Task      EnqueueAsync(string sessionId, string input, string? model);
    Task<int> GetPendingCountAsync();
    Task      ProcessQueueAsync(CancellationToken ct = default);
    Task      ResetProcessingItemsAsync();
}
