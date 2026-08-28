using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using LocalAIAssistant.Converters;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using LocalAIAssistant.ViewModels;
using Microsoft.Maui.Graphics;
using Moq;
using Xunit;

namespace LaaUnitTests;

public class LoggingSubsystemAndFilterTests
{
    [Fact]
    public void LogEntry_LevelBadgeText_FormatsCorrectly()
    {
        var errEntry  = new LogEntry { Level = "Error" };
        var warnEntry = new LogEntry { Level = "Warning" };
        var infoEntry = new LogEntry { Level = "Information" };
        var dbgEntry  = new LogEntry { Level = "Debug" };

        Assert.Equal("ERR", errEntry.LevelBadgeText);
        Assert.Equal("WRN", warnEntry.LevelBadgeText);
        Assert.Equal("INF", infoEntry.LevelBadgeText);
        Assert.Equal("DBG", dbgEntry.LevelBadgeText);
    }

    [Fact]
    public void LogEntry_HasException_DetectsExceptionPresence()
    {
        var noEx   = new LogEntry { Exception = null };
        var withEx = new LogEntry { Exception = "System.InvalidOperationException: Boom!\n   at Method()" };

        Assert.False(noEx.HasException);
        Assert.True(withEx.HasException);
    }

    [Fact]
    public void LogEntry_PropertiesFormatted_FormatsKeyValues()
    {
        var entry = new LogEntry
        {
            Properties = new Dictionary<string, string>
            {
                { "Subsystem", "Chat" },
                { "DurationMs", "450" }
            }
        };

        var formatted = entry.PropertiesFormatted;

        Assert.Contains("Subsystem: Chat", formatted);
        Assert.Contains("DurationMs: 450", formatted);
    }

    [Fact]
    public void LogLevelToColorConverter_ReturnsExpectedColors()
    {
        var converter = new LogLevelToColorConverter();

        var errColor  = (Color)converter.Convert("Error", typeof(Color), null, CultureInfo.InvariantCulture);
        var warnColor = (Color)converter.Convert("Warning", typeof(Color), null, CultureInfo.InvariantCulture);
        var infoColor = (Color)converter.Convert("Information", typeof(Color), null, CultureInfo.InvariantCulture);
        var dbgColor  = (Color)converter.Convert("Debug", typeof(Color), null, CultureInfo.InvariantCulture);

        Assert.Equal(Color.FromArgb("#E53935"), errColor);
        Assert.Equal(Color.FromArgb("#FB8C00"), warnColor);
        Assert.Equal(Color.FromArgb("#1E88E5"), infoColor);
        Assert.Equal(Color.FromArgb("#78909C"), dbgColor);
    }

    [Fact]
    public async Task LogsViewModel_FiltersByLevel_Correctly()
    {
        var mockLoggingService = new Mock<ILoggingService>();
        var sampleLogs = new List<LogEntry>
        {
            new LogEntry { Id = 1, Level = "Information", Category = "Chat", Message = "Chat initialized" },
            new LogEntry { Id = 2, Level = "Warning",     Category = "Network", Message = "High latency" },
            new LogEntry { Id = 3, Level = "Error",       Category = "Sync", Message = "Failed to sync", Exception = "TimeoutException" },
            new LogEntry { Id = 4, Level = "Debug",       Category = "Audio", Message = "Sample buffer loaded" }
        };

        mockLoggingService.Setup(service => service.GetLogEntriesAsync())
                          .ReturnsAsync(sampleLogs);

        var viewModel = new LogsViewModel(mockLoggingService.Object);
        await viewModel.LoadLogs();

        Assert.Equal(4, viewModel.TotalCount);
        Assert.Equal(1, viewModel.ErrorCount);
        Assert.Equal(1, viewModel.WarningCount);
        Assert.Equal(1, viewModel.InfoCount);
        Assert.Equal(1, viewModel.DebugCount);
        Assert.Equal(4, viewModel.LogEntries.Count);

        // Filter: Errors
        viewModel.SetLevelFilter("Error");
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("Failed to sync", viewModel.LogEntries[0].Message);

        // Filter: Warnings
        viewModel.SetLevelFilter("Warning");
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("High latency", viewModel.LogEntries[0].Message);

        // Filter: All
        viewModel.SetLevelFilter("All");
        Assert.Equal(4, viewModel.LogEntries.Count);
    }

    [Fact]
    public async Task LogsViewModel_FiltersByCategory_Correctly()
    {
        var mockLoggingService = new Mock<ILoggingService>();
        var sampleLogs = new List<LogEntry>
        {
            new LogEntry { Id = 1, Level = "Information", Category = "Chat", Message = "Chat message sent" },
            new LogEntry { Id = 2, Level = "Information", Category = "Network", Message = "Endpoint pinged" },
            new LogEntry { Id = 3, Level = "Error",       Category = "Network", Message = "Connection refused" }
        };

        mockLoggingService.Setup(service => service.GetLogEntriesAsync())
                          .ReturnsAsync(sampleLogs);

        var viewModel = new LogsViewModel(mockLoggingService.Object);
        await viewModel.LoadLogs();

        Assert.Contains("Chat", viewModel.Categories);
        Assert.Contains("Network", viewModel.Categories);

        viewModel.SelectedCategory = "Network";
        Assert.Equal(2, viewModel.LogEntries.Count);

        viewModel.SelectedCategory = "Chat";
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("Chat message sent", viewModel.LogEntries[0].Message);

        viewModel.SelectedCategory = "All Categories";
        Assert.Equal(3, viewModel.LogEntries.Count);
    }

    [Fact]
    public async Task LogsViewModel_FiltersBySearchText_AcrossFields()
    {
        var mockLoggingService = new Mock<ILoggingService>();
        var sampleLogs = new List<LogEntry>
        {
            new LogEntry { Id = 1, Level = "Information", Category = "Chat", Message = "Executing command /meal" },
            new LogEntry { Id = 2, Level = "Error",       Category = "Database", Message = "Disk failure", Exception = "SqliteException: Database locked" },
            new LogEntry { Id = 3, Level = "Warning",     Category = "Storage", Message = "Free disk space below 500MB" }
        };

        mockLoggingService.Setup(service => service.GetLogEntriesAsync())
                          .ReturnsAsync(sampleLogs);

        var viewModel = new LogsViewModel(mockLoggingService.Object);
        await viewModel.LoadLogs();

        // Search message text
        viewModel.SearchText = "meal";
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("Executing command /meal", viewModel.LogEntries[0].Message);

        // Search exception text
        viewModel.SearchText = "Database locked";
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("Disk failure", viewModel.LogEntries[0].Message);

        // Search category text
        viewModel.SearchText = "Storage";
        Assert.Single(viewModel.LogEntries);
        Assert.Equal("Free disk space below 500MB", viewModel.LogEntries[0].Message);

        // Clear search
        viewModel.SearchText = string.Empty;
        Assert.Equal(3, viewModel.LogEntries.Count);
    }
}
