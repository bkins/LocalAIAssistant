using System;
using System.Collections.Generic;

namespace LocalAIAssistant.Core.ConversationRecorder;

public class ConversationMemoryCandidateDto
{
    public Guid         Id                         { get; set; } = Guid.NewGuid();
    public Guid         ConversationId             { get; set; }
    public Guid?        AnalysisId                 { get; set; }
    public string       Category                   { get; set; } = "Fact";
    public string       Content                    { get; set; } = string.Empty;
    public string?      Speaker                    { get; set; }
    public List<Guid>   SourceTranscriptSegmentIds { get; set; } = new();
    public double       Confidence                 { get; set; } = 1.0;
    public DateTime     ExtractedAtUtc             { get; set; } = DateTime.UtcNow;
    public string       State                      { get; set; } = "Provisional";
    public bool         IsDeleted                  { get; set; }
    public DateTime?    DeletedUtc                 { get; set; }
}
