using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Coco.Models;

/// <summary>
/// Unified SSE event from POST /rag/ask-stream.
/// Covers both ProgressUpdate (stage != "complete") and CompletionUpdate (stage == "complete").
/// Heartbeat events are identified by a non-null Status field.
/// </summary>
public class CocoAskEvent
{
    [JsonPropertyName("stage")]     public string?                 Stage     { get; init; }
    [JsonPropertyName("detail")]    public string?                 Detail    { get; init; }
    [JsonPropertyName("response")]  public string?                 Response  { get; init; }
    [JsonPropertyName("current")]   public int?                    Current   { get; init; }
    [JsonPropertyName("total")]     public int?                    Total     { get; init; }
    [JsonPropertyName("timestamp")] public DateTime                Timestamp { get; init; }
    [JsonPropertyName("status")]    public string?                 Status    { get; init; }
    [JsonPropertyName("sources")]   public IReadOnlyList<string>?  Sources   { get; init; }

    public bool IsHeartbeat => string.Equals(Status, "Heartbeat", StringComparison.OrdinalIgnoreCase);
    public bool IsComplete  => string.Equals(Stage,  "complete",  StringComparison.OrdinalIgnoreCase);
}
