using System.ComponentModel;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.Services;

public class ApiHealthService : INotifyPropertyChanged, IDisposable
{
    private          bool       _isApiAvailable;
    private          Timer      _timer;
    private readonly HttpClient _httpClient;
    private readonly ICognitivePlatformClientFactory _cognitivePlatformClientFactory;
    
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

    public ApiHealthService(HttpClient httpClient,  ICognitivePlatformClientFactory cognitivePlatformClientFactory)
    {
        _httpClient = httpClient;
        _cognitivePlatformClientFactory = cognitivePlatformClientFactory;
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
            var cpClient = _cognitivePlatformClientFactory.Create();
            var response = await cpClient.Ready(); // await _httpClient.GetAsync(StringConsts.OllamaServerUrl);

            IsApiAvailable = response.IsSuccessStatusCode;
        }
        catch(Exception ex)
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
