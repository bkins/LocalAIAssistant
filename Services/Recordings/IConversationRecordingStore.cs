using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAIAssistant.Services.Recordings;

public interface IConversationRecordingStore
{
    Task<string> SaveAsync( ConversationRecording recording
                          , CancellationToken     cancellationToken = default );

    Task<ConversationRecording?> GetByIdAsync( string            id
                                              , CancellationToken cancellationToken = default );

    Task<IReadOnlyList<ConversationRecording>> GetAllAsync( CancellationToken cancellationToken = default );

    Task<bool> SoftDeleteAsync( string            id
                              , CancellationToken cancellationToken = default );
}
