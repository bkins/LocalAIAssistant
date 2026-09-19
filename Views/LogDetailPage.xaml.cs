using System;
using System.Threading.Tasks;
using CP.Client.Core.Avails;
using LocalAIAssistant.Services.Logging;
using LocalAIAssistant.Services.Logging.Interfaces;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.Views;

[QueryProperty(nameof(Entry), nameof(LogEntry))]
public partial class LogDetailPage : ContentPage
{
    private readonly ILoggingService _loggingService;
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

    public LogDetailPage(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        InitializeComponent();
    }

    private void OnSelectableEditorLoaded(object? sender, EventArgs eventArgs)
    {
#if ANDROID
        if (sender is Editor editor
         && editor.Handler?.PlatformView is Android.Widget.EditText nativeEditor)
        {
            // MAUI's Android read-only mapping suppresses the focus/long-press state
            // required for selection. Restore selection without opening the keyboard.
            nativeEditor.SetTextIsSelectable(true);
            nativeEditor.LongClickable = true;
            nativeEditor.ShowSoftInputOnFocus = false;
        }
#endif
    }

    private async void OnDeleteEntryClicked(object? sender, EventArgs eventArgs)
    {
        if (_entry is null) return;

        var excerpt = _entry.Message.ReplaceLineEndings(" ").Trim();
        if (excerpt.Length > 120) excerpt = excerpt[..117] + "...";
        var confirmed = await DisplayAlert("Delete log entry?"
                                         , $"{_entry.DisplayTimestamp} [{_entry.LevelBadgeText}]\n{excerpt}"
                                         , "Delete"
                                         , "Cancel");
        if (!confirmed) return;

        DeleteEntryButton.IsEnabled = false;
        DeleteEntryButton.Text = "Deleting...";
        try
        {
            var deleted = await _loggingService.DeleteLogEntryAsync(_entry);
            if (!deleted)
            {
                await DisplayAlert("Entry not deleted"
                                 , "This entry was already removed or is no longer present. No other log entry was changed."
                                 , "OK");
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            await DisplayAlert("Unable to delete log entry", exception.Message, "OK");
        }
        finally
        {
            DeleteEntryButton.IsEnabled = true;
            DeleteEntryButton.Text = "Delete this log entry";
        }
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
