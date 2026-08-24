namespace LocalAIAssistant.Core.ConversationRecorder;

public interface IConversationRecorderApiClient
{
    Task<TranscriptDto?> TranscribeRecordingAsync( Guid conversationId, Stream audioStream, string mimeType = "audio/wav", CancellationToken cancellationToken = default );
    Task<TranscriptDto?> DiarizeRecordingAsync( Guid conversationId, Stream audioStream, CancellationToken cancellationToken = default );
    Task<TranscriptDto?> GetTranscriptAsync( Guid conversationId, CancellationToken cancellationToken = default );
}
