using System;
using System.Threading.Tasks;
using CP.Client.Core.Avails;
using LocalAIAssistant.Services.Logging;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.Views;

[QueryProperty(nameof(Entry), nameof(LogEntry))]
public partial class LogDetailPage : ContentPage
{
    private LogEntry? _entry;
    public LogEntry? Entry
    {
        get => _entry;
        set
        {
            _entry         = value;
            BindingContext = _entry;
        }
    }

    public LogDetailPage()
    {
        InitializeComponent();
    }

    private async void OnCopyMessageClicked(object? sender, EventArgs e)
    {
        if (_entry?.Message.HasValue() == true)
        {
            await Clipboard.Default.SetTextAsync(_entry.Message);
            await DisplayAlert("Copied", "Message text copied to clipboard.", "OK");
        }
    }

    private async void OnCopyExceptionClicked(object? sender, EventArgs e)
    {
        if (_entry?.Exception.HasValue() == true)
        {
            await Clipboard.Default.SetTextAsync(_entry.Exception);
            await DisplayAlert("Copied", "Exception stack trace copied to clipboard.", "OK");
        }
    }

    private async void OnCopyRawJsonClicked(object? sender, EventArgs e)
    {
        var text = _entry?.PrettifiedFullText ?? _entry?.FullText;
        if (text.HasValue())
        {
            await Clipboard.Default.SetTextAsync(text);
            await DisplayAlert("Copied", "Raw event JSON copied to clipboard.", "OK");
        }
    }
}