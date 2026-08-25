using System;

namespace LocalAIAssistant.Services.Recordings;

public class ConversationRecording
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Title { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EndedAt { get; set; }

    public TimeSpan Duration { get; set; }

    public string RecordingPath { get; set; } = string.Empty;

    public string Status { get; set; } = "Recorded";

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedUtc { get; set; }
}
