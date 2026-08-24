using System;

namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationParticipantDto
{
    public Guid    Id             { get; set; }
    public Guid    ConversationId { get; set; }
    public string  SpeakerId      { get; set; } = string.Empty;
    public string? DisplayName    { get; set; }
}
