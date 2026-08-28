namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationDetailsDto
{
    public required ConversationRecordDto         Record       { get; set; }
    public          TranscriptDto?                 Transcript   { get; set; }
    public          List<ConversationParticipantDto> Participants { get; set; } = new();
    public          ConversationAnalysisDto?         Analysis     { get; set; }
}
