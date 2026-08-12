using System;

namespace LocalAIAssistant.CognitivePlatform.DTOs;

public enum AgentJobStatus
{
    Unknown
  , Pending
  , Running
  , Completed
  , Failed
}

public class AgentJobDto
{
    public string          Id             { get; set; } = string.Empty;
    public string          Prompt         { get; set; } = string.Empty;
    public AgentJobStatus  Status         { get; set; } = AgentJobStatus.Pending;
    public string?         Response       { get; set; }
    public string?         ConversationId { get; set; }
    public DateTimeOffset  CreatedUtc     { get; set; }
    public DateTimeOffset? StartedUtc     { get; set; }
    public DateTimeOffset? CompletedUtc   { get; set; }
    public string?         Error          { get; set; }
}
