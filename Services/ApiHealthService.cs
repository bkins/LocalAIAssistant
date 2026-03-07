using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Data;

namespace LocalAIAssistant.Services;

public class ApiHealthService : INotifyPropertyChanged, IDisposable
{
    private readonly ICognitivePlatformClientFactory _cognitivePlatformClientFactory;
    
    private readonly Stopwatch _stopwatch;

    private bool  _isApiAvailable;
    private Timer _timer;
    private bool  _isInitialized;
    
    public TimeSpan TimeSinceLastCheck => _stopwatch.Elapsed;
    
    public bool IsApiAvailable
    {
        get => _isApiAvailable;
        set
        {
            if (_isApiAvailable == value) return;
            
            _isApiAvailable = value;
            OnPropertyChanged();
        }
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        private set
        {
            if (_isInitialized == value) return;

            _isInitialized = value;
            OnPropertyChanged();
        }
    }
    
    public ApiHealthService(ICognitivePlatformClientFactory cognitivePlatformClientFactory)
    {
        _cognitivePlatformClientFactory = cognitivePlatformClientFactory;

        _stopwatch = new Stopwatch();
    }
    
    public async Task InitializeAsync()
    {
        // Check immediately
        await CheckApiAsync();
        
        var waitBeforeFirstRepeat = TimeSpan.FromMinutes(5);
        var repeatEveryInMinutes  = TimeSpan.FromMinutes(5);
        
        // Set up a recurring check every 5 minutes
        _timer = new Timer(async void (_) =>
                           {
                               try
                               {
                                   await CheckApiAsync();
                               }
                               catch (Exception e)
                               {
                                   IsApiAvailable = false;
                               }
                           }
                         , null
                         , waitBeforeFirstRepeat
                         , repeatEveryInMinutes);

        IsInitialized = true;
    }

    public async Task CheckApiAsync([CallerMemberName] string memberName = "")
    {
        try
        {
            if (_stopwatch.IsRunning)
                _stopwatch.Restart();
            else 
                _stopwatch.Start();
            
            var cpClient = _cognitivePlatformClientFactory.Create();
            var response = await cpClient.Ping(memberName);

            IsApiAvailable = response.IsSuccessStatusCode;
        }
        catch(Exception ex)
        {
            IsApiAvailable = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this
                              , new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _timer.Dispose();
    }
}
