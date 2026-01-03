using System.Runtime.CompilerServices;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.AiMemory.Interfaces;

namespace LocalAIAssistant.Services.AiMemory;

public interface IShortTermMemoryStore : IAiMemoryStore
{

    Task                       SaveMessagesAsync(IEnumerable<Message> messages);
    Task<IEnumerable<Message>> LoadMessagesAsync();
    Task                       ClearMemoryAsync();
    Task                       DeleteMessagesOlderThanAsync(DateTime cutoffUtc, [CallerMemberName] string caller = null);

}

public interface ILongTermMemoryStore : IAiMemoryStore
{

    Task                       SaveMessageAsync(Message               message);
    Task                       SaveMessagesAsync(IEnumerable<Message> messages);
    Task<IEnumerable<Message>> LoadMessagesAsync();
    Task<IEnumerable<Message>> GetMessagesSinceAsync(DateTime? since = null);
    Task                       ClearMemoryAsync();

    Task<IEnumerable<Message>> SearchMessagesAsync(string query, int    limit = 20);

}