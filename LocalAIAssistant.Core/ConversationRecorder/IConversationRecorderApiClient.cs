namespace LocalAIAssistant.Core.ConversationRecorder;

public interface IConversationRecorderApiClient
{
    Task<TranscriptDto?> TranscribeRecordingAsync( Guid conversationId, Stream audioStream, string mimeType = "audio/wav", CancellationToken cancellationToken = default );
    Task<TranscriptDto?> DiarizeRecordingAsync( Guid conversationId, Stream audioStream, CancellationToken cancellationToken = default );
    Task<TranscriptDto?> MapParticipantsAsync( Guid conversationId, Dictionary<string, string> speakerMap, CancellationToken cancellationToken = default );
    Task<List<ConversationParticipantDto>> GetParticipantsAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<ConversationDetailsDto?> GetConversationDetailsAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<List<ConversationRecordDto>> SearchConversationsAsync( string? query = null, string? participant = null, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken cancellationToken = default );
    Task<bool> UploadAudioAsync( Guid conversationId, Stream audioStream, string mimeType = "audio/wav", CancellationToken cancellationToken = default );
    Task<Stream?> GetAudioStreamAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<ConversationAnalysisDto?> AnalyzeConversationAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<ConversationAnalysisDto?> GetAnalysisAsync( Guid conversationId, CancellationToken cancellationToken = default );
}
