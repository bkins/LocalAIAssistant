using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.Services;

public enum ApiEnvironment
{
    QaAndroid
  , QaLocal
  , Qa
  , Dev
}

public partial class ApiEnvironmentService : ObservableObject
{
    const string StorageKey = "ApiEnvironment";

    [ObservableProperty] private bool           _isInitialized = false;
    [ObservableProperty] private ApiEnvironment current;

    public string BaseUrl => ResolveUrl();

    string ResolveUrl() =>
            Current switch
            {
                    ApiEnvironment.Qa        => "http://192.168.0.33:5000"
                  , ApiEnvironment.QaAndroid => "http://10.0.2.2:5000"
                  , ApiEnvironment.QaLocal   => "http://localhost:5000"
                  , ApiEnvironment.Dev       => "http://192.168.0.33:5273"
                  , _                        => throw new ArgumentOutOfRangeException()
            };

    public Task InitializeAsync(ApiEnvironment defaultEnvironment, bool forceDefault =  false)
    {
        if (IsInitialized) return Task.CompletedTask;

        if (forceDefault) Preferences.Set(StorageKey, defaultEnvironment.ToString());
    
        var saved = Preferences.Get(StorageKey, defaultEnvironment.ToString());
    
        if (!Enum.TryParse(saved, out ApiEnvironment env))
            env = defaultEnvironment;
    
        Current = env;
        IsInitialized = true;
    
        return Task.CompletedTask;
    }


    public async Task SetAsync (ApiEnvironment env)
    {
        if (Current == env) return;

        Current = env;
        Preferences.Set(StorageKey
                      , env.ToString());
    }
}
