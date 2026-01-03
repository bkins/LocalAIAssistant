using Microsoft.Extensions.Logging;

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
    // // Trace
    // void LogTrace(string messageTemplate, Category category = Category.Unknown, params object[] propertyValues);
    //
    // // Information
    // void LogInformation(string messageTemplate, Category category = Category.Unknown, params object[] propertyValues);
    //
    // // Warning
    // void LogWarning(string messageTemplate, Category category = Category.Unknown, params object[] propertyValues);
    //
    // // Error
    // void LogError(Exception ex, string messageTemplate, Category category = Category.Unknown, params object[] propertyValues);
    //
    // // Retrieve log entries (parsed from JSON log file)
    // Task<List<LogEntry>> GetLogEntriesAsync();
    //
    // // Clear all logs
    // Task ClearLogsAsync();
}


// namespace LocalAIAssistant.Services.Logging;
//
// public interface ILoggingService
// {
//     void                 LogTrace(string       message,         string?         category = null);
//     void                 LogTrace(string       messageTemplate, params object[] propertyValues);
//     void                 LogInformation(string message,         Category?       category = null);
//     void                 LogInformation(string messageTemplate, params object[] propertyValues);
//     void                 LogWarning(string     message,         Category?       category = null);
//     void                 LogWarning(string     messageTemplate, params object[] propertyValues);
//     void                 LogError(Exception    ex,              string          messageTemplate, params object[] propertyValues);
//     void                 LogError(Exception    ex,              string?         moreInformation);
//     Task<List<LogEntry>> GetLogEntriesAsync();
//     Task                 ClearLogsAsync();
//
//
// }