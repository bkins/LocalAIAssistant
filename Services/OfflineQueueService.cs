using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Contracts;
using LocalAIAssistant.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIAssistant.Services;

public class OfflineQueueService: IOfflineQueueService
{
    public event EventHandler<QueueProcessedEventArgs>? QueueProcessed;

    private readonly IServiceProvider             _serviceProvider;
    private readonly CognitivePlatformClientBase _apiClient;

    public OfflineQueueService( IServiceProvider                serviceProvider
                              , ICognitivePlatformClientFactory clientFactory  )
    {
        _serviceProvider = serviceProvider;
        _apiClient        = clientFactory.Create();
    }

    public async Task EnqueueAsync(string sessionId, string input, string? model)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalAiAssistantDbContext>();

        var item = new OfflineQueueItem
                   {
                           Id              = Guid.NewGuid()
                         , ClientRequestId = Guid.NewGuid()
                         , SessionId       = sessionId
                         , Input           = input
                         , Model           = model
                         , CreatedUtc      = DateTime.UtcNow
                         , Status          = OfflineQueueStatus.Pending
                   };

        db.OfflineQueue.Add(item);
        
        try
        {
            await db.SaveChangesAsync();
            QueueProcessed?.Invoke(this, new QueueProcessedEventArgs(0));
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save offline queue item due to database error.", ex);
        }
    }
    
    public async Task<int> GetPendingCountAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalAiAssistantDbContext>();

        return await db.OfflineQueue
                        .CountAsync(x => x.Status == OfflineQueueStatus.Pending);
    }

    public async Task ProcessQueueAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalAiAssistantDbContext>();

        var items = await db.OfflineQueue
                             .Where(x => x.Status == OfflineQueueStatus.Pending)
                             .OrderBy(x => x.CreatedUtc)
                             .ToListAsync(ct);

        var replayedCount = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            item.Status = OfflineQueueStatus.Processing;
            await db.SaveChangesAsync(ct);

            try
            {
                var request = new ConverseRequest
                              {
                                      SessionId       = item.SessionId
                                    , Input           = item.Input
                                    , Model           = item.Model
                                    , ClientRequestId = item.ClientRequestId
                              };

                var response = await _apiClient.ConverseAsync(request.Input
                                                            , request.SessionId
                                                            , request.Model ?? string.Empty);

                // If success:
                db.OfflineQueue.Remove(item);
                await db.SaveChangesAsync(ct);
                replayedCount++;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.Net.Sockets.SocketException)
            {
                item.Status = OfflineQueueStatus.Pending;
                item.RetryCount++;
                await db.SaveChangesAsync(ct);

                // Stop processing further items if the API fails
                break;
            }
            catch (Exception)
            {
                item.Status = OfflineQueueStatus.Pending;
                item.RetryCount++;
                await db.SaveChangesAsync(ct);
                break;
            }
        }

        QueueProcessed?.Invoke(this, new QueueProcessedEventArgs(replayedCount));
    }


    public async Task ResetProcessingItemsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalAiAssistantDbContext>();

        var stuckItems = await db.OfflineQueue
                                  .Where(queueItem => queueItem.Status == OfflineQueueStatus.Processing)
                                  .ToListAsync();

        foreach (var item in stuckItems)
            item.Status = OfflineQueueStatus.Pending;

        await db.SaveChangesAsync();
    }

}