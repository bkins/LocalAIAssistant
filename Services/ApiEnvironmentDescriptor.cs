using CommunityToolkit.Mvvm.ComponentModel;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Logging;

namespace LocalAIAssistant.Services;

public partial class ApiEnvironmentDescriptor : ObservableObject
{
    [ObservableProperty] private string  _name;
    [ObservableProperty] private string  _baseUrl;
    [ObservableProperty] private string  _ollamaUrl;

    public ApiEnvironmentDescriptor(string name, string baseUrl,  string ollamaUrl)
    {
        Name      = name;
        BaseUrl   = baseUrl.TrimEnd('/');
        OllamaUrl = ollamaUrl.TrimEnd('/');
    }
}