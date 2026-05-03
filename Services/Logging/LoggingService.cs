using System.Text.Json;
using System.Text.Json.Serialization;
using LocalAIAssistant.Extensions;
using System.Text;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace LocalAIAssistant.Services.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger _logger;
    private readonly string  _logFilePath;
    
    public LoggingService(ILogger logger
                        , string  logFilePath)
    {
        _logger      = logger;
        _logFilePath = logFilePath;
    }

    // 🔹 Core unified logger
    private void Log(LogLevel        level
                   , string          messageTemplate
                   , Category?       category
                   , params object[] propertyValues)
    {
        var loggerToUse = _logger;

        if (category.HasValue && category != Category.Unknown)
        {
            loggerToUse = loggerToUse.ForContext("Category"
                                               , category.Value);
        }

        var serilogLevel = LogLevelExtensions.ToSerilogLevel(level);
        loggerToUse.Write(serilogLevel
                        , messageTemplate
                        , propertyValues);
    }

    public void LogTrace(string          messageTemplate
                       , Category?       category = null
                       , params object[] propertyValues)
    {
        Log(LogLevel.Trace
          , messageTemplate
          , category
          , propertyValues);
    }

    public void LogInformation(string          messageTemplate
                             , Category?       category = null
                             , params object[] propertyValues)
    {
        Log(LogLevel.Information
          , messageTemplate
          , category
          , propertyValues);
    }

    public void LogWarning(string          messageTemplate
                         , Category?       category = null
                         , params object[] propertyValues)
    {
        Log(LogLevel.Warning
          , messageTemplate
          , category
          , propertyValues);
    }

    public void LogError(Exception       ex
                       , string          messageTemplate
                       , Category?       category = null
                       , params object[] propertyValues)
    {
        var loggerToUse = _logger;
        if (category.HasValue && category != Category.Unknown)
        {
            loggerToUse = loggerToUse.ForContext("Category"
                                               , category.Value);
        }

        loggerToUse.Error(ex
                        , messageTemplate
                        , propertyValues);
    }

    public async Task<List<LogEntry>> GetLogEntriesAsync()
    {
        var logEntries = new List<LogEntry>();

        if (File.Exists(_logFilePath).Not())
            return logEntries;

        try
        {
            using var stream = new FileStream(_logFilePath
                                            , FileMode.Open
                                            , FileAccess.Read
                                            , FileShare.ReadWrite);
            
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.HasNoValue()) continue;

                try
                {
                    var logEvent = JsonSerializer.Deserialize<SerilogLogEvent>(line);
                    if (logEvent != null)
                    {
                        var sb = new StringBuilder();

                        if (logEvent.Properties.TryGetValue("Category"
                                                          , out var categoryElement)
                         && categoryElement.ValueKind == JsonValueKind.String)
                        {
                            sb.Append($"[{categoryElement.GetString()}] ");
                        }

                        sb.Append(logEvent.RenderedMessage ?? logEvent.MessageTemplate);

                        logEntries.Add(new LogEntry
                                       {
                                           Timestamp       = logEvent.Timestamp.ToLocalTime()
                                         , Level           = logEvent.Level ?? "Information"
                                         , Message         = sb.ToString()
                                         , RenderedMessage = sb.ToString()
                                         , FullText        = line
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
            LogError(ex
                   , "Failed to read log entries"
                   , Category.LoggingService);
        }

        return logEntries.OrderBy(e => e.Timestamp).ToList();
    }

    public async Task ClearLogsAsync()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                await File.WriteAllTextAsync(_logFilePath
                                           , string.Empty);
            }
        }
        catch (Exception ex)
        {
            LogError(ex
                   , "Failed to clear logs"
                   , Category.LoggingService);
        }
    }
}

// Serilog log event for JSON parsing
public class SerilogLogEvent
{
    [JsonPropertyName("@t")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("@mt")]
    public string MessageTemplate { get; set; } = string.Empty;

    [JsonPropertyName("@l")]
    public string? Level { get; set; }

    [JsonPropertyName("@r")]
    public string? RenderedMessage { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties { get; set; } = new();

    [JsonPropertyName(nameof(Message))]
    public string Message { get; set; } = string.Empty;
}

public enum Category
{

    Unknown
  , Orchestrator
  , MemoryService
  , SqliteAiMemoryStore
  , App
  , HybridIntentAnalyzer
  , LoggingService
  , CognitivePlatformClient
   , MainPage
}

public class LogEntry
{
    public int      Id                 { get; set; }
    public DateTime Timestamp          { get; set; }
    public string   Level              { get; set; } = string.Empty;
    public string   Message            { get; set; } = string.Empty;
    public string   RenderedMessage    { get; set; } = string.Empty;
    public string   FullText           { get; set; } = string.Empty;
    public string   PrettifiedFullText => Prettify();
    public string   DisplayText        => $"[{Level}] {Message}";
    public string   FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    public string Prettify() => Prettify(FullText);

    public static string Prettify(string json)
    {
        if (json.HasNoValue()) return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc,
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json; // Not valid JSON, return original
        }
    }
}