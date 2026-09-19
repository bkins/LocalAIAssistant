using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using LocalAIAssistant.Extensions;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;
using CP.Shared.Primitives.Avails.Extensions;

namespace LocalAIAssistant.Services.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger              _logger;
    private readonly string               _logFilePath;
    private readonly DeletedLogEntryStore _deletedEntryStore;
    
    public LoggingService(ILogger logger
                        , string  logFilePath)
    {
        _logger            = logger;
        _logFilePath       = logFilePath;
        _deletedEntryStore = new DeletedLogEntryStore($"{logFilePath}.deleted");
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
        var page = await GetLogPageAsync(0, 200);
        return page.Entries.ToList();
    }

    public async Task<LogPage> GetLogPageAsync( int               offset
                                              , int               pageSize
                                              , CancellationToken cancellationToken = default)
    {
        offset   = Math.Max(0, offset);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var deletedIds        = await _deletedEntryStore.GetAllAsync(cancellationToken);
        var requestedLineCount = (int)Math.Min(int.MaxValue, (long)offset + pageSize + deletedIds.Count + 1);
        var records            = await Task.Run(() => NewestLogLineReader.ReadRecords(_logFilePath, requestedLineCount, cancellationToken), cancellationToken);
        var availableRecords = records.Where(record => !deletedIds.Contains(record.StorageId)).ToList();
        var selected = availableRecords.Skip(offset).Take(pageSize).ToList();
        var entries = new List<LogEntry>();
        var malformed = 0;

        if (File.Exists(_logFilePath).Not())
            return new LogPage(entries, offset, false, 0);

        foreach (var record in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = record.Text;
            if (line.HasNoValue()) continue;
            try
            {
                var logEvent = JsonSerializer.Deserialize<SerilogLogEvent>(line);
                if (logEvent != null)
                {
                    var category = "General";
                    var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (key, element) in logEvent.Properties)
                    {
                        if (key.EqualsIgnoreCase("Category"))
                        {
                            category = element.GetString() ?? category;
                        }
                        else if (key.EqualsIgnoreCase("SourceContext"))
                        {
                            var sourceContext = element.GetString() ?? string.Empty;
                            if (category == "General" && sourceContext.HasValue())
                            {
                                category = sourceContext.Split('.').Last();
                            }
                        }
                        else properties[key] = element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.ToString();
                    }

                    var messageText = logEvent.RenderedMessage ?? logEvent.RenderedMessageCompact ?? logEvent.MessageTemplate ?? logEvent.Message;
                    entries.Add(new LogEntry
                                {
                                    Id              = offset + entries.Count + 1
                                  , StorageId       = record.StorageId
                                  , StorageOffset   = record.Offset
                                  , Timestamp       = logEvent.Timestamp.ToLocalTime()
                                  , Level           = logEvent.Level ?? "Information"
                                  , Category        = category
                                  , Message         = messageText
                                  , RenderedMessage = messageText
                                  , Exception       = logEvent.Exception
                                  , Properties      = properties
                                  , FullText        = line
                                });
                }
            }
            catch (JsonException) { malformed++; }
        }

        return new LogPage(entries.OrderByDescending(entry => entry.Timestamp).ToList(), offset, availableRecords.Count > offset + pageSize, malformed);
    }

    public Task<bool> DeleteLogEntryAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        // Serilog actively owns the JSONL file. A durable deletion index avoids
        // rewriting that file and losing records appended during the operation.
        return _deletedEntryStore.AddAsync(_logFilePath
                                         , entry.StorageId
                                         , entry.StorageOffset
                                         , entry.FullText
                                         , cancellationToken);
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
            await _deletedEntryStore.ClearAsync();
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

    [JsonPropertyName("@m")]
    public string? RenderedMessageCompact { get; set; }

    [JsonPropertyName("@l")]
    public string? Level { get; set; }

    [JsonPropertyName("@r")]
    public string? RenderedMessage { get; set; }

    [JsonPropertyName("@x")]
    public string? Exception { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Properties { get; set; } = new();

    [JsonPropertyName(nameof(Message))]
    public string Message { get; set; } = string.Empty;
}
