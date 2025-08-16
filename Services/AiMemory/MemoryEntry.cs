namespace LocalAIAssistant.Services.AiMemory;

public class MemoryEntry
{
    public string   Role      { get; set; } = string.Empty; // "AI" or "User"
    public DateTime Timestamp { get; set; }
    public string   Content   { get; set; } = string.Empty;
}