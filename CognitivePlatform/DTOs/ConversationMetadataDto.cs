using System;

namespace LocalAIAssistant.CognitivePlatform.DTOs;

public class ConversationMetadataDto
{
    public string   ConversationId { get; set; } = string.Empty;
    public string?  Name           { get; set; }
    public DateTime CreatedUtc     { get; set; }
    public DateTime LastActiveUtc  { get; set; }
    public int      MessageCount   { get; set; }
    public bool     IsDeleted      { get; set; }
}
