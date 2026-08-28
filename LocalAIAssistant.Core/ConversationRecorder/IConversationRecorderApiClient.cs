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
    Task<List<ConversationMemoryCandidateDto>?> ExtractMemoriesAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<List<ConversationMemoryCandidateDto>?> GetMemoriesAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<bool> ConfirmMemoryAsync( Guid conversationId, Guid memoryId, CancellationToken cancellationToken = default );
    Task<List<ConversationMemoryCandidateDto>?> QueryMemoriesAsync( string query, CancellationToken cancellationToken = default );
    Task<CopilotSliceResultDto?> ProcessCopilotSliceAsync( Guid conversationId, Stream audioStream, int sliceIndex, double offsetSeconds, double durationSeconds, string? contextWindowText = null, string mimeType = "audio/wav", CancellationToken cancellationToken = default );
    Task<List<CopilotInsightDto>?> GetCopilotInsightsAsync( Guid conversationId, CancellationToken cancellationToken = default );
    Task<bool> DismissCopilotInsightAsync( Guid conversationId, Guid insightId, CancellationToken cancellationToken = default );
}
