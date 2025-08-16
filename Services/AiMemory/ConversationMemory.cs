using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory.Interfaces;

namespace LocalAIAssistant.Services.AiMemory;

public class ConversationMemory : IConversationMemory
{

    private readonly IShortTermMemoryStore _shortTermMemoryStore; // SQLIte
    private readonly ILongTermMemoryStore  _longTermMemoryStore; // JSONL

    private readonly List<Message> _currentConversationMemory = new();

    public ConversationMemory(IShortTermMemoryStore shortTemMemoryStore
                            , ILongTermMemoryStore longTermMemoryStore)
    {
        _shortTermMemoryStore = shortTemMemoryStore ?? throw new ArgumentNullException(nameof(shortTemMemoryStore));
        _longTermMemoryStore  = longTermMemoryStore ?? throw new ArgumentNullException(nameof(longTermMemoryStore));
    }

    public async Task InitializeAsync()
    {
        // 1. This does not seem to be right.  Should I be adding the short term memory to the current conversation?
        // 2. Should I load the short term memory into a public property instead?
        // 3. If so, should I load the long term memory as well?
        var persistedMessages = await _shortTermMemoryStore.LoadMessagesAsync();
        _currentConversationMemory.Clear();
        _currentConversationMemory.AddRange(persistedMessages);
    }

    public async Task AddAsync(Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        _currentConversationMemory.Add(message);

        // Always write to short-term (for fast recall) and long-term (for permanent history)
        await _shortTermMemoryStore.SaveMessagesAsync(new[] { message });
        await _longTermMemoryStore.SaveMessagesAsync(new[] { message });
    }

    public IEnumerable<Message> GetRecentEntries(int count)
    {
        if (count <= 0) return Enumerable.Empty<Message>();
        return _currentConversationMemory.TakeLast(count);
    }

    public async Task<IEnumerable<Message>> GetEntriesSince(DateTime since)
    {
        //return await _currentConversationMemory.Where(m => m.Timestamp >= since);
        throw new NotImplementedException();
    }

    public async Task SaveAsync()
    {
        await _shortTermMemoryStore.SaveMessagesAsync(_currentConversationMemory);
        await _longTermMemoryStore.SaveMessagesAsync(_currentConversationMemory);
    }

    public Task ClearAsync()
    {
        // Just clears in-memory session history
        _currentConversationMemory.Clear();
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Message>> LoadShortTermAsync() => _shortTermMemoryStore.LoadMessagesAsync();

    public Task<IEnumerable<Message>> LoadLongTermAsync() => _longTermMemoryStore.LoadMessagesAsync();

    public Task ClearLongTermAsync() => _longTermMemoryStore.ClearMemoryAsync();


    public Task ClearShortTermAsync() => _shortTermMemoryStore.ClearMemoryAsync();

}
