namespace LocalAIAssistant.Services.AiMemory.Interfaces;

public interface IMemoryService
{
    Task                      AddMemoryAsync(MemoryType              type, string key, string value);
    Task<string?>             GetMemoryAsync(MemoryType              type, string key);
    Task<IEnumerable<string>> GetAllMemoriesAsync(MemoryType         type);
    Task                      RemoveMemoryAsync(MemoryType           type, string key);
    Task                      ClearMemoryAsync(MemoryType            type);
    void                      StoreMemory(string                     role,  string content, DateTime timestamp, double importance = 0.5);
    IEnumerable<MemoryEntry>  RetrieveRelevantMemories(string        query, int    maxResults = 5);
    void                      ForgetMemories(Func<MemoryEntry, bool> predicate);
    Task<MemoryContext>       GetContextForTurnAsync(string          userInput, MemoryRetrievalOptions opts, CancellationToken ct = default);

    Task SaveEntryAsync(string   ai
                      , string   finalAssistantMessage
                      , DateTime utcNow);

}