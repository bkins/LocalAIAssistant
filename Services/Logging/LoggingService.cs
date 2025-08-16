using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LocalAIAssistant.Services.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly string                  _logFilePath;// = Path.Combine(FileSystem.AppDataDirectory, "logs", "app-log-.jsonl");

    
    public LoggingService(ILogger<LoggingService> logger, string logFilePath)
    {
        _logger = logger;
        _logFilePath = logFilePath;
    }

    public void LogTrace(string message, string? category = null)
    {
        if (string.IsNullOrEmpty(category))
            _logger.LogTrace(message);
        else
            _logger.LogTrace("[{Category}] {Message}", category, message);
    }

    public void LogInformation(string message, string? category = null)
    {
        if (string.IsNullOrEmpty(category))
            _logger.LogInformation(message);
        else
            _logger.LogInformation("[{Category}] {Message}", category, message);
    }

    public void LogWarning(string message, string? category = null)
    {
        if (string.IsNullOrEmpty(category))
            _logger.LogWarning(message);
        else
            _logger.LogWarning("[{Category}] {Message}", category, message);
    }

    public void LogError(Exception ex, string? category = null)
    {
        if (string.IsNullOrEmpty(category))
            _logger.LogError(ex, ex.Message);
        else
            _logger.LogError(ex, "[{Category}] {Message}", category, ex.Message);
    }
    
    public async Task<List<LogEntry>> GetLogEntriesAsync()
    {
        var logEntries = new List<LogEntry>();

        if (!File.Exists(_logFilePath))
            return logEntries;

        try
        {
            var lines = await File.ReadAllLinesAsync(_logFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var logEvent = JsonSerializer.Deserialize<SerilogLogEvent>(line);
                    if (logEvent != null)
                    {
                        logEntries.Add(new LogEntry
                                       {
                                           Timestamp = logEvent.Timestamp,
                                           Level     = logEvent.Level ?? "Information",
                                           Message   = logEvent.RenderedMessage,
                                           FullText  = line
                                       });
                    }
                }
                catch
                {
                    // ignore invalid lines
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read log entries");
        }

        return logEntries.OrderBy(e => e.Timestamp).ToList();
    }
    
    public async Task ClearLogsAsync()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                // Clear file content asynchronously
                using var stream = new FileStream(_logFilePath, FileMode.Truncate);
                await stream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear logs");
        }
    }

}

public class SerilogLogEvent
{
    [JsonPropertyName("@t")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("@mt")]
    public string MessageTemplate { get; set; } = string.Empty;

    [JsonPropertyName("@l")]
    public string? Level { get; set; }

    public string RenderedMessage => MessageTemplate;
}

public class LogEntry
{
    public int      Id        { get; set; }
    public DateTime Timestamp { get; set; }
    public string   Level     { get; set; } = string.Empty;
    public string   Message   { get; set; } = string.Empty;
    public string   FullText  { get; set; } = string.Empty;
    
    public string DisplayText => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
} 