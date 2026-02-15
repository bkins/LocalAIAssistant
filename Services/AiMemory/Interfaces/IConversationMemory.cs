using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.AiMemory.Interfaces;

public interface IConversationMemory
{
    Task                       InitializeAsync();
    Task                       AddAsync(Message         message);
    Task<IEnumerable<Message>> GetEntriesSince(DateTime since);
    Task                       ClearAsync();
    Task<IEnumerable<Message>> LoadShortTermAsync();
    Task<IEnumerable<Message>> LoadLongTermAsync();

    Task ClearLongTermAsync();

    Task ClearShortTermAsync();

    Task<IEnumerable<Message>> GetRecentEntries (int optsMaxStmMessages);

}