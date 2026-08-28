using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CP.Client.Core.Avails;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace LocalAIAssistant.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;
    private readonly List<LogEntry> _allEntries = new();

    [ObservableProperty] private ObservableCollection<LogEntry> _logEntries = new();
    [ObservableProperty] private ObservableCollection<string>   _categories = new() { "All Categories" };
    [ObservableProperty] private bool                           _isLoading;
    [ObservableProperty] private bool                           _hasError;
    [ObservableProperty] private string                         _errorMessage = string.Empty;
    [ObservableProperty] private LogEntry?                      _selectedLogEntry;

    [ObservableProperty] private string _searchText          = string.Empty;
    [ObservableProperty] private string _selectedLevelFilter  = "All";
    [ObservableProperty] private string _selectedCategory     = "All Categories";

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _infoCount;
    [ObservableProperty] private int _debugCount;

    public bool HasLogs => LogEntries.Count > 0;
    public bool IsEmpty => !IsLoading && LogEntries.Count == 0;

    public LogsViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedLevelFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    [RelayCommand]
    public async Task LoadLogs()
    {
        try
        {
            IsLoading = true;
            HasError  = false;

            var rawLogs = await _loggingService.GetLogEntriesAsync();
            _allEntries.Clear();
            _allEntries.AddRange(rawLogs);

            UpdateMetrics();
            UpdateCategories();
            ApplyFilters();

            WeakReferenceMessenger.Default.Send(new LogErrorsChangedMessage(ErrorCount > 0));
        }
        catch (Exception ex)
        {
            HasError     = true;
            ErrorMessage = $"Failed to load logs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasLogs));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public void SetLevelFilter(string level)
    {
        SelectedLevelFilter = level ?? "All";
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        IEnumerable<LogEntry> query = _allEntries;

        if (SelectedLevelFilter.EqualsIgnoreCase("Error") || SelectedLevelFilter.EqualsIgnoreCase("Errors"))
        {
            query = query.Where(entry => entry.Level.EqualsIgnoreCase("Error") || entry.Level.EqualsIgnoreCase("Critical") || entry.Level.EqualsIgnoreCase("Fatal"));
        }
        else if (SelectedLevelFilter.EqualsIgnoreCase("Warning") || SelectedLevelFilter.EqualsIgnoreCase("Warnings"))
        {
            query = query.Where(entry => entry.Level.EqualsIgnoreCase("Warning") || entry.Level.EqualsIgnoreCase("Warn"));
        }
        else if (SelectedLevelFilter.EqualsIgnoreCase("Information") || SelectedLevelFilter.EqualsIgnoreCase("Info"))
        {
            query = query.Where(entry => entry.Level.EqualsIgnoreCase("Information") || entry.Level.EqualsIgnoreCase("Info"));
        }
        else if (SelectedLevelFilter.EqualsIgnoreCase("Debug") || SelectedLevelFilter.EqualsIgnoreCase("Trace"))
        {
            query = query.Where(entry => entry.Level.EqualsIgnoreCase("Debug") || entry.Level.EqualsIgnoreCase("Trace") || entry.Level.EqualsIgnoreCase("Dbg"));
        }

        if (SelectedCategory.HasValue() && !SelectedCategory.EqualsIgnoreCase("All Categories"))
        {
            query = query.Where(entry => entry.Category.EqualsIgnoreCase(SelectedCategory));
        }

        if (SearchText.HasValue())
        {
            var search = SearchText.Trim();
            query = query.Where(entry => (entry.Message.HasValue() && entry.Message.ContainsIgnoreCase(search))
                                      || (entry.RenderedMessage.HasValue() && entry.RenderedMessage.ContainsIgnoreCase(search))
                                      || (entry.Category.HasValue() && entry.Category.ContainsIgnoreCase(search))
                                      || (entry.Exception.HasValue() && entry.Exception.ContainsIgnoreCase(search))
                                      || (entry.FullText.HasValue() && entry.FullText.ContainsIgnoreCase(search)));
        }

        var filteredList = query.OrderByDescending(entry => entry.Timestamp).ToList();

        LogEntries.Clear();
        foreach (var entry in filteredList)
        {
            LogEntries.Add(entry);
        }

        OnPropertyChanged(nameof(HasLogs));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void UpdateMetrics()
    {
        TotalCount   = _allEntries.Count;
        ErrorCount   = _allEntries.Count(entry => entry.Level.EqualsIgnoreCase("Error") || entry.Level.EqualsIgnoreCase("Critical") || entry.Level.EqualsIgnoreCase("Fatal"));
        WarningCount = _allEntries.Count(entry => entry.Level.EqualsIgnoreCase("Warning") || entry.Level.EqualsIgnoreCase("Warn"));
        InfoCount    = _allEntries.Count(entry => entry.Level.EqualsIgnoreCase("Information") || entry.Level.EqualsIgnoreCase("Info"));
        DebugCount   = _allEntries.Count(entry => entry.Level.EqualsIgnoreCase("Debug") || entry.Level.EqualsIgnoreCase("Trace") || entry.Level.EqualsIgnoreCase("Dbg"));
    }

    private void UpdateCategories()
    {
        var existing = SelectedCategory;
        var distinctCategories = _allEntries.Select(entry => entry.Category)
                                            .Where(category => category.HasValue())
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .OrderBy(category => category)
                                            .ToList();

        Categories.Clear();
        Categories.Add("All Categories");
        foreach (var category in distinctCategories)
        {
            Categories.Add(category);
        }

        if (Categories.Contains(existing))
        {
            SelectedCategory = existing;
        }
        else
        {
            SelectedCategory = "All Categories";
        }
    }

    [RelayCommand]
    public async Task ClearLogs()
    {
        try
        {
            IsLoading = true;
            HasError  = false;

            await _loggingService.ClearLogsAsync();
            _allEntries.Clear();
            LogEntries.Clear();
            UpdateMetrics();
            UpdateCategories();
            WeakReferenceMessenger.Default.Send(new LogErrorsChangedMessage(false));
        }
        catch (Exception ex)
        {
            HasError     = true;
            ErrorMessage = $"Failed to clear logs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasLogs));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public async Task RefreshLogs()
    {
        await LoadLogs();
    }

    [RelayCommand]
    public async Task CopyAllLogs()
    {
        try
        {
            if (LogEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine($"=== LocalAIAssistant Diagnostic Logs Export ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            sb.AppendLine($"Total Filtered Entries: {LogEntries.Count}");
            sb.AppendLine();

            foreach (var entry in LogEntries)
            {
                sb.AppendLine($"[{entry.FormattedTimestamp}] [{entry.LevelBadgeText}] [{entry.Category}] {entry.Message}");
                if (entry.HasException)
                {
                    sb.AppendLine("Exception Details:");
                    sb.AppendLine(entry.Exception);
                }
                if (entry.Properties.Count > 0)
                {
                    sb.AppendLine($"Properties: {entry.PropertiesFormatted}");
                }
                sb.AppendLine(new string('-', 80));
            }

            await Clipboard.Default.SetTextAsync(sb.ToString());
        }
        catch (Exception ex)
        {
            HasError     = true;
            ErrorMessage = $"Failed to copy logs: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ExportLogs()
    {
        try
        {
            if (LogEntries.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine($"=== LocalAIAssistant Diagnostic Logs Export ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
            sb.AppendLine($"Total Filtered Entries: {LogEntries.Count}");
            sb.AppendLine();

            foreach (var entry in LogEntries)
            {
                sb.AppendLine($"[{entry.FormattedTimestamp}] [{entry.LevelBadgeText}] [{entry.Category}] {entry.Message}");
                if (entry.HasException)
                {
                    sb.AppendLine("Exception Details:");
                    sb.AppendLine(entry.Exception);
                }
                if (entry.Properties.Count > 0)
                {
                    sb.AppendLine($"Properties: {entry.PropertiesFormatted}");
                }
                sb.AppendLine(new string('-', 80));
            }

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "LocalAIAssistant Diagnostic Logs",
                Text  = sb.ToString()
            });
        }
        catch (Exception ex)
        {
            HasError     = true;
            ErrorMessage = $"Failed to export logs: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task TestLogging()
    {
        try
        {
            _loggingService.LogInformation("Diagnostic health check initiated across all subsystems.", Category.App);
            _loggingService.LogInformation("Cognitive Platform API connection established on http://localhost:5000", Category.General);
            _loggingService.LogWarning("Network latency threshold exceeded (response time: 842ms).", Category.Network);

            try
            {
                throw new InvalidOperationException("Simulated background synchronization failure for testing stack trace rendering.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Handled synchronization error in OfflineQueueService", Category.LoggingService);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await Task.Delay(150, cts.Token);
            await LoadLogs();
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
            if (entry == null) return;

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("LogDetailPage", new Dictionary<string, object> { { "LogEntry", entry } });
            }
            SelectedLogEntry = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LogSelected error: {ex}");
        }
    }

    async partial void OnSelectedLogEntryChanged(LogEntry? value)
    {
        if (value is null) return;

        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("LogDetailPage", new Dictionary<string, object>
                {
                    { "LogEntry", value }
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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
