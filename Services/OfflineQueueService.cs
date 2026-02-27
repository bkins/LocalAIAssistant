using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Contracts;
using LocalAIAssistant.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalAIAssistant.Services;

// Skeleton

// TODO: Complete implementation of an offline queue service that can be used to queue up tasks or messages when the system is offline,
//  and process them when connectivity is restored.
public class OfflineQueueService: IOfflineQueueService
{
    private readonly LocalAiAssistantDbContext _db;
    private readonly ICognitivePlatformClient _apiClient;

    public OfflineQueueService( LocalAiAssistantDbContext       db
                              , ICognitivePlatformClientFactory clientFactory  )
    {
        _db        = db;
        _apiClient = clientFactory.Create();
    }

    public async Task EnqueueAsync(string sessionId, string input, string? model)
    {
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

        _db.OfflineQueue.Add(item);
        
        await _db.SaveChangesAsync();
    }
    
    public async Task<int> GetPendingCountAsync()
    {
        return await _db.OfflineQueue
                        .CountAsync(x => x.Status == OfflineQueueStatus.Pending);
    }

    public async Task ProcessQueueAsync(CancellationToken ct = default)
    {
        var items = await _db.OfflineQueue
                             .Where(x => x.Status == OfflineQueueStatus.Pending)
                             .OrderBy(x => x.CreatedUtc)
                             .ToListAsync(ct);

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            item.Status = OfflineQueueStatus.Processing;
            await _db.SaveChangesAsync(ct);

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
                _db.OfflineQueue.Remove(item);
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                item.Status = OfflineQueueStatus.Pending;
                item.RetryCount++;
                await _db.SaveChangesAsync(ct);

                // Stop processing further items if the API fails
                break;
            }
        }
    }


    public async Task ResetProcessingItemsAsync()
    {
        var stuckItems = await _db.OfflineQueue
                                  .Where(queueItem => queueItem.Status == OfflineQueueStatus.Processing)
                                  .ToListAsync();

        foreach (var item in stuckItems)
            item.Status = OfflineQueueStatus.Pending;

        await _db.SaveChangesAsync();
    }

}