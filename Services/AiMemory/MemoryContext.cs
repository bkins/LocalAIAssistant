using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.AiMemory;

public class MemoryContext
{
    public string                 Summary { get; init; } = string.Empty;
    public IReadOnlyList<Message> StmUsed { get; init; } = new List<Message>();
    public IReadOnlyList<Message> LtmUsed { get; init; } = new List<Message>();
}