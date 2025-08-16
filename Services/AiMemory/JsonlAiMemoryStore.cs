using LocalAIAssistant.Data.Models;

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
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await File.AppendAllTextAsync(_filePath, json + Environment.NewLine);
    }

    public async Task<IEnumerable<Message>> LoadAllAsync()
    {
        if (!File.Exists(_filePath))
            return Enumerable.Empty<Message>();

        var lines = await File.ReadAllLinesAsync(_filePath);
        return lines.Where(value => ! string.IsNullOrWhiteSpace(value))
                    .Select(json => System.Text.Json.JsonSerializer.Deserialize<Message>(json)!);
    }

    public async Task SaveMessagesAsync(IEnumerable<Message> messages)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return;

        var lines = messageList.Select(message => System.Text.Json.JsonSerializer.Serialize(message));

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

    public async Task<IEnumerable<Message>> LoadMessagesAsync()
    {
        if ( ! File.Exists(_filePath)) return Enumerable.Empty<Message>();

        var lines = await File.ReadAllLinesAsync(_filePath).ConfigureAwait(false);
        return lines.Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(json => System.Text.Json.JsonSerializer.Deserialize<Message>(json)!)
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
