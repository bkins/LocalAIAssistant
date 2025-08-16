namespace LocalAIAssistant.Services.AiMemory;

public class AiMemoryEntry
{
    public string   Role      { get; set; } = string.Empty; // "user", "assistant", "system"
    public string   Content   { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}