using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Views;

namespace LocalAIAssistant.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private LogEntry? _selectedLogEntry;

    public LogsViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [RelayCommand]
    private async Task LoadLogs()
    {
        try
        {
            IsLoading = true;
            HasError = false;

            var logs = await _loggingService.GetLogEntriesAsync();
            
            LogEntries.Clear();
            foreach (var log in logs)
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
    private async Task ClearLogs()
    {
        try
        {
            IsLoading = true;
            HasError = false;

            await _loggingService.ClearLogsAsync();
            await LoadLogs();
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
            // Test both Serilog and direct file writing
            _loggingService.LogInformation("This is a test information message");
            _loggingService.LogWarning("This is a test warning message");
            _loggingService.LogError(new Exception("Test Exception"), "This is a test error message");
            
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