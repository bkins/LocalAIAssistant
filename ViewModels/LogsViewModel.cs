using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Views;
using CommunityToolkit.Mvvm.Messaging;

namespace LocalAIAssistant.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = new();
    [ObservableProperty] private bool                           _isLoading;
    [ObservableProperty] private bool                           _hasError;
    [ObservableProperty] private string                         _errorMessage = string.Empty;
    [ObservableProperty] private LogEntry?                      _selectedLogEntry;

    // Filter properties
    [ObservableProperty] private bool _showInformation = true;
    [ObservableProperty] private bool _showWarning     = true;
    [ObservableProperty] private bool _showError       = true;

    public LogsViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        LogEntries.CollectionChanged += (sender, args) =>
        {
            if (LogEntries.Any(e => e.Level == "Error"))
            {
                WeakReferenceMessenger.Default.Send(new LogErrorsChangedMessage(true));
            }
        };
    }

    [RelayCommand]
    private async Task LoadLogs()
    {
        try
        {
            IsLoading = true;
            HasError = false;

            var logs = await _loggingService.GetLogEntriesAsync();
            
            // Apply the filters
            var filteredLogs = logs.Where(log => (ShowInformation && log.Level == "Information")
                                              || (ShowWarning && log.Level == "Warning")
                                              || (ShowError && log.Level == "Error"))
                                   .OrderByDescending(log=>log.Timestamp)
                                   .ToList();

            LogEntries.Clear();
            foreach (var log in filteredLogs)
            {
                LogEntries.Add(log);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load logs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task ApplyFilters()
    {
        await LoadLogs();
    }
    
    [RelayCommand]
    private async Task ClearLogs()
    {
        try
        {
            IsLoading = true;
            HasError = false;

            await _loggingService.ClearLogsAsync();
            await LoadLogs();
            WeakReferenceMessenger.Default.Send(new LogErrorsChangedMessage(false)); // Reset the status
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to clear logs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLogs()
    {
        await LoadLogs();
    }

    [RelayCommand]
    private async Task TestLogging()
    {
        try
        {
            var test = "test value";
            
            // Test both Serilog and direct file writing
            _loggingService.LogInformation($"This is a test information message [{test}]", Category.App);
            _loggingService.LogInformation("This is a test information message",           Category.App);
            
            // Pass the message template and the value separately
            _loggingService.LogWarning("This is a test warning message [{Test}]", Category.App, test);

            // If you have a different log message that doesn't need additional properties
            _loggingService.LogWarning("This is a test message without a value.", Category.App);
            
            _loggingService.LogError(new Exception("Test Exception"), $"This is a test error message [{test}]", Category.App);
            _loggingService.LogError(new Exception("Test Exception"), "This is a test error message", Category.App);
            
            // Also write directly to file to test
            // _loggingService.WriteDirectToFile("This is a direct file write test");
            
            // Force Serilog to flush (this will close the logger, so we need to be careful)
            // Instead, let's just wait a moment for the logs to be written
            await Task.Delay(100);
            
            // Also log to the built-in logger for debugging
            System.Diagnostics.Debug.WriteLine("Test logging completed");
            _ = LoadLogs();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in TestLogging: {ex.Message}");
        }
    }
    [RelayCommand]
    private async Task LogSelected(SelectionChangedEventArgs args)
    {
        try
        {
            var entry = args?.CurrentSelection?.FirstOrDefault() as LogEntry;
            if (entry == null)
                return;

            // Navigate and pass the object via Shell route values
            await Shell.Current.GoToAsync(nameof(LogDetailPage),
                                          new Dictionary<string, object> { { "LogEntry", entry } });

            // Clear selection so user can tap the same row again later
            SelectedLogEntry = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LogSelected error: {ex}");
        }
    }

    async partial void OnSelectedLogEntryChanged(LogEntry? value)
    {
        if (value is null)
            return;

        // Fire and forget navigation — this is an event handler style method
        try
        {
            await Shell.Current.GoToAsync(nameof(LogDetailPage),
                                        new Dictionary<string, object>
                                        {
                                            { "LogEntry", value }
                                        });

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        // Clear selection so user can tap same log again later
        SelectedLogEntry = null;
    }
} 

public class LogErrorsChangedMessage
{
    public bool HasErrors { get; }

    public LogErrorsChangedMessage(bool hasErrors)
    {
        HasErrors = hasErrors;
    }
}
