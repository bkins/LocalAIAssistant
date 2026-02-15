
namespace LocalAIAssistant.Services.Logging;

public interface ILoggingService
{
    // Trace
    void LogTrace(string messageTemplate, Category? category = null, params object[] propertyValues);

    // Information
    void LogInformation(string messageTemplate, Category? category = null, params object[] propertyValues);

    // Warning
    void LogWarning(string messageTemplate, Category? category = null, params object[] propertyValues);

    // Error
    void LogError(Exception ex, string messageTemplate, Category? category = null, params object[] propertyValues);

    // Retrieve log entries (parsed from JSON log file)
    Task<List<LogEntry>> GetLogEntriesAsync();

    // Clear all logs
    Task ClearLogsAsync();
}