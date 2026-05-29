using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.Views;

[QueryProperty(nameof(Entry), nameof(LogEntry))]
public partial class LogDetailPage : ContentPage
{
    private LogEntry _entry;
    public LogEntry Entry
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
        
        //BindingContext = entry;
    }

    private async void OnLogTapped( object?         sender
                            , TappedEventArgs e )
    {
        if (sender is not Label label)
            return;

        var message = label.BindingContext;

        var contentProp = message?.GetType().GetProperty("FullText");
        var text        = contentProp?.GetValue(message)?.ToString();

        if (string.IsNullOrWhiteSpace(text))
            return;

        await Clipboard.Default.SetTextAsync(text);

        // Optional: UX feedback
        await DisplayToast("Copied to clipboard");
    }
    
    private async Task DisplayToast(string message)
    {
        await DisplayAlert("", message, "OK");
    }
}