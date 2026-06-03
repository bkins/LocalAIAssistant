using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Coco.Models;

/// <summary>
/// SSE event from POST /rag/index-stream (IndexProgressUpdate serialized by Coco).
/// </summary>
public class CocoIndexEvent
{
    [JsonPropertyName("status")]       public string?  Status       { get; init; }
    [JsonPropertyName("message")]      public string?  Message      { get; init; }
    [JsonPropertyName("currentFile")]  public string?  CurrentFile  { get; init; }
    [JsonPropertyName("processed")]    public int?     Processed    { get; init; }
    [JsonPropertyName("total")]        public int?     Total        { get; init; }
    [JsonPropertyName("timestamp")]    public DateTime Timestamp    { get; init; }
    [JsonPropertyName("operation")]    public string?  Operation    { get; init; }
    [JsonPropertyName("correlationId")] public string? CorrelationId { get; init; }

    public bool IsCompleted => string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase);
    public bool IsError     => string.Equals(Status, "Error",     StringComparison.OrdinalIgnoreCase);
}
