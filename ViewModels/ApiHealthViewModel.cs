using CP.Client.Core.Avails;
using LocalAIAssistant.Services;

namespace LocalAIAssistant.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public class ApiHealthViewModel : INotifyPropertyChanged
{
    private readonly ApiHealthService _apiHealthService;

    public ApiHealthViewModel(ApiHealthService apiHealthService)
    {
        _apiHealthService     = apiHealthService;
        CheckApiStatusCommand = new Command(async () => await CheckApiStatusAsync());
    }

    private bool _isApiAvailable;
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

    private string _timeSinceLastCheck;

    public string TimeSinceLastCheck
    {
        get => _timeSinceLastCheck;
        set
        {
            if (_timeSinceLastCheck == value) return;
            
            _timeSinceLastCheck = value;
            OnPropertyChanged();
        }
    }

    public ICommand CheckApiStatusCommand { get; }

    public async Task CheckApiStatusAsync()
    {
        //await _apiHealthService.InitializeAsync();
        await _apiHealthService.CheckApiAsync().ConfigureAwait(false);
        IsApiAvailable = _apiHealthService.IsApiAvailable;
        
        var elapsed = _apiHealthService.TimeSinceLastCheck;
        TimeSinceLastCheck = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}:{elapsed.Milliseconds:000}";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
