using System.Text.Json.Serialization;

namespace LocalAIAssistant.Core.Coco.Models;

public class CocoHealthResponse
{
    [JsonPropertyName("success")]   public bool        Success   { get; init; }
    [JsonPropertyName("error")]     public string?     Error     { get; init; }
    [JsonPropertyName("timestamp")] public string?     Timestamp { get; init; }
    [JsonPropertyName("data")]      public CocoHealthData? Data  { get; init; }
}

public class CocoHealthData
{
    [JsonPropertyName("apiAvailable")]      public bool    ApiAvailable      { get; init; }
    [JsonPropertyName("ollamaAvailable")]   public bool    OllamaAvailable   { get; init; }
    [JsonPropertyName("storageAvailable")]  public bool    StorageAvailable  { get; init; }
    [JsonPropertyName("indexingAvailable")] public bool    IndexingAvailable { get; init; }
    [JsonPropertyName("message")]           public string? Message           { get; init; }
}
