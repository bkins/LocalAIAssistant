using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;
using System.Text.Json;
using LocalAIAssistant.Services.AiMemory.Interfaces;

namespace LocalAIAssistant.Services.AiMemory;

public class JsonlAiMemoryStore : ILongTermMemoryStore
{
    private readonly string _filePath;

    public JsonlAiMemoryStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task SaveMessageAsync(Message message)
    {
        var json = JsonSerializer.Serialize(message);
        await File.AppendAllTextAsync(_filePath, json + Environment.NewLine);
    }

    public async Task<IEnumerable<Message>> LoadMessagesAsync(DateTime? since = null
                                                            , int?      limit = null)
    {
        if (File.Exists(_filePath).Not()) return [];

        var lines = await File.ReadAllLinesAsync(_filePath).ConfigureAwait(false);
        
        var messages = lines.Where(value => value.HasValue())
                            .Select(json => JsonSerializer.Deserialize<Message>(json)!)
                            .OrderByDescending(message => message.Timestamp)
                            .ToList();
        
        if (since.HasValue)
        {
            messages = messages.Where(message => message.Timestamp >= since.Value).ToList();
        }
        
        return limit.HasValue 
                    ? messages.Take(limit.Value).ToList() 
                    : messages;
    }

    public Task DeleteMessageAsync(Guid id)
    {
        throw new NotImplementedException();
    }
    
    public async Task<IEnumerable<Message>> SearchMessagesAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<Message>();
        
        var needle = query.ToLowerInvariant();
        
        var all = await LoadMessagesAsync().ConfigureAwait(false);
        return all.Where(m => (m.Content ?? "").ToLowerInvariant().Contains(needle))
                  .Take(limit);
    }

    public async Task<IEnumerable<Message>> LoadAllAsync()
    {
        if (File.Exists(_filePath).Not())
            return Enumerable.Empty<Message>();

        var lines = await File.ReadAllLinesAsync(_filePath);
        return lines.Where(value => value.HasValue())
                    .Select(json => JsonSerializer.Deserialize<Message>(json)!);
    }

    public async Task SaveMessagesAsync(IEnumerable<Message> messages)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return;

        var lines = messageList.Select(message => JsonSerializer.Serialize(message));

        var content = string.Join(Environment.NewLine, lines) + Environment.NewLine;

        await File.AppendAllTextAsync(_filePath, content);
    }

    public Task ClearMemoryAsync()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
        return Task.CompletedTask;
    }

    Task<IEnumerable<Message>> IAiMemoryStore.LoadMessagesAsync() => throw new NotImplementedException();

    Task<IEnumerable<Message>> IAiMemoryStore.GetMessagesSinceAsync(DateTime? since) => throw new NotImplementedException();

    public async Task<IEnumerable<Message>> LoadMessagesAsync()
    {
        if (File.Exists(_filePath).Not()) return Enumerable.Empty<Message>();

        var lines = await File.ReadAllLinesAsync(_filePath).ConfigureAwait(false);
        return lines.Where(value => value.HasValue())
                    .Select(json => JsonSerializer.Deserialize<Message>(json)!)
                    .ToList();
    }

    public async Task<IEnumerable<Message>> GetMessagesSinceAsync(DateTime? since = null)
    {
        var allMessages = await LoadMessagesAsync().ConfigureAwait(false);

        if (since is null)
            return allMessages;

        return allMessages.Where(message => message.Timestamp >= since.Value);
    }

}
