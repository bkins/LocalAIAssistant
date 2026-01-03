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
}