namespace LocalAIAssistant.Services.Interfaces;

public interface IOfflineQueueService
{
    Task   EnqueueAsync(string sessionId, string input, string? model);
    Task<int> GetPendingCountAsync();
    Task      ProcessQueueAsync(CancellationToken ct = default);
    Task      ResetProcessingItemsAsync();
}
