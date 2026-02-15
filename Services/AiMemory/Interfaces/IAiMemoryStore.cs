using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.AiMemory.Interfaces;

public interface IAiMemoryStore
{
    Task SaveMessagesAsync(IEnumerable<Message> messages);
    Task SaveMessageAsync(Message               message);
    Task ClearMemoryAsync();
    
    Task<IEnumerable<Message>> LoadMessagesAsync();
    Task<IEnumerable<Message>> GetMessagesSinceAsync(DateTime? since = null);

}