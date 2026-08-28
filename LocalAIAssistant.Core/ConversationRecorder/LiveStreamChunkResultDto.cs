using System;
using System.Collections.Generic;

namespace LocalAIAssistant.Core.ConversationRecorder;

public sealed class LiveStreamChunkResultDto
{
    public Guid                       ConversationId       { get; set; }
    public int                        ChunkIndex           { get; set; }
    public TranscriptSegmentDto?      Segment              { get; set; }
    public bool                       IsFinal              { get; set; }
    public List<CopilotInsightDto>    Insights             { get; set; } = new();
    public bool                       HasActionableInsight => Insights != null && Insights.Count > 0;
    public Dictionary<string, double> SpeakerTalkTime      { get; set; } = new();
    public DateTime                   ProcessedAtUtc       { get; set; } = DateTime.UtcNow;
}
