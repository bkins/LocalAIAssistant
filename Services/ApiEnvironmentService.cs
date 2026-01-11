using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.Services;

public enum ApiEnvironment
{
    Qa,
    Dev
}

public partial class ApiEnvironmentService : ObservableObject
{
    const string StorageKey = "ApiEnvironment";

    [ObservableProperty] private ApiEnvironment current;

    public string BaseUrl => ResolveUrl();

    string ResolveUrl() =>
            Current switch
            {
                    ApiEnvironment.Dev => "http://192.168.0.33:5273", _ => "http://192.168.0.33:5272"
            };

    public async Task InitializeAsync()
    {
        var saved = Preferences.Get(StorageKey
                                  , ApiEnvironment.Qa.ToString());

        if (!Enum.TryParse(saved
                         , out ApiEnvironment env))
            env = ApiEnvironment.Qa;

        Current = env;
    }

    public async Task SetAsync (ApiEnvironment env)
    {
        if (Current == env) return;

        Current = env;
        Preferences.Set(StorageKey
                      , env.ToString());
    }
}
