namespace LocalAIAssistant.Services.Logging;

public interface ILoggingService
{
    void                 LogTrace(string message, string? category = null);
    void                 LogInformation(string message, string? category = null);
    void                 LogWarning(string message, string? category = null);
    void                 LogError(Exception ex, string? category = null);
    Task<List<LogEntry>> GetLogEntriesAsync();
    Task                 ClearLogsAsync();
}