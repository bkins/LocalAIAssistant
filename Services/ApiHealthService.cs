using System.ComponentModel;

namespace LocalAIAssistant.Services;

public class ApiHealthService : INotifyPropertyChanged, IDisposable
{
    private          bool       _isApiAvailable;
    private          Timer      _timer;
    private readonly HttpClient _httpClient;
    
    public bool IsApiAvailable
    {
        get => _isApiAvailable;
        set
        {
            if (_isApiAvailable != value)
            {
                _isApiAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    public ApiHealthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task InitializeAsync()
    {
        // Check immediately
        await CheckApiAsync();
        var waitBeforeFirstRepeat = TimeSpan.FromMinutes(5);
        var repeatEvery_InMinutes = TimeSpan.FromMinutes(5);
        
        // Set up recurring check every 5 minutes
        _timer = new Timer(async void (_) => await CheckApiAsync()
                         , null
                         , waitBeforeFirstRepeat
                         , repeatEvery_InMinutes
        );
    }

    public async Task CheckApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://192.168.0.33:11434/");
            IsApiAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            IsApiAvailable = false;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
