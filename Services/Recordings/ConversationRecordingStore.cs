using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CognitivePlatform.Api.Data;

namespace LocalAIAssistant.Services.Recordings;

public class ConversationRecordingStore : IConversationRecordingStore
{
    private const string PartitionKey = "conversation_recordings";
    private readonly IObjectStore _objectStore;

    public ConversationRecordingStore(IObjectStore objectStore)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
    }

    public Task<string> SaveAsync( ConversationRecording recording
                                  , CancellationToken     cancellationToken = default )
    {
        if (recording == null)
        {
            throw new ArgumentNullException(nameof(recording));
        }

        var savedId = _objectStore.Save(recording, PartitionKey, recording.Id);
        return Task.FromResult(savedId);
    }

    public Task<ConversationRecording?> GetByIdAsync( string            id
                                                      , CancellationToken cancellationToken = default )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<ConversationRecording?>(null);
        }

        var recording = _objectStore.Get<ConversationRecording>(id, PartitionKey);
        return Task.FromResult(recording);
    }

    public Task<IReadOnlyList<ConversationRecording>> GetAllAsync( CancellationToken cancellationToken = default )
    {
        var recordings = _objectStore.List<ConversationRecording>(PartitionKey)
                                     .Where(item => !item.IsDeleted)
                                     .OrderByDescending(item => item.StartedAt)
                                     .ToList();

        return Task.FromResult<IReadOnlyList<ConversationRecording>>(recordings);
    }

    public Task<bool> SoftDeleteAsync( string            id
                                      , CancellationToken cancellationToken = default )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(false);
        }

        var recording = _objectStore.Get<ConversationRecording>(id, PartitionKey);
        if (recording == null)
        {
            return Task.FromResult(false);
        }

        recording.IsDeleted = true;
        recording.DeletedUtc = DateTimeOffset.UtcNow;
        _objectStore.Save(recording, PartitionKey, id);

        return Task.FromResult(true);
    }
}
