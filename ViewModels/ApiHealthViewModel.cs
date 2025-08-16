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
            if (_isApiAvailable != value)
            {
                _isApiAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand CheckApiStatusCommand { get; }

    public async Task CheckApiStatusAsync()
    {
        await _apiHealthService.CheckApiAsync().ConfigureAwait(false);
        IsApiAvailable = _apiHealthService.IsApiAvailable;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
