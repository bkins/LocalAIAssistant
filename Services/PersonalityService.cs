using System.Diagnostics;
using LocalAIAssistant.Core.Personality;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Personalities;
using LocalAIAssistant.Services.Interfaces;

namespace LocalAIAssistant.Services;

public class PersonalityService : IPersonalityService
{
    private readonly List<Personality>                 _personalities;
    private readonly OllamaConfigService               _ollamaConfigService;
    private readonly IEnumerable<IPersonalityProvider> _personalityProviders;
    private readonly IPersonalityApiClient             _apiClient;

    public Personality Current { get; private set; }

    public PersonalityService( IEnumerable<IPersonalityProvider> providers
                             , OllamaConfigService               configService
                             , IPersonalityApiClient             apiClient )
    {
        _ollamaConfigService  = configService;
        _personalityProviders = providers;
        _apiClient            = apiClient;
        _personalities        = new List<Personality>();

        foreach (var provider in _personalityProviders)
        {
            _personalities.AddRange(provider.Load());

            if (provider is JsonPersonalityProvider jsonProvider)
                jsonProvider.OnReload += ReloadAll;
        }

        SetDefault();
    }

    public void SetCurrent(Personality personality)
    {
        Current = personality;
        ApplyModelConfig(Current);

        _ = _apiClient?.ActivateAsync(personality.Id)
                        .ContinueWith(task => Debug.WriteLine($"[PersonalityService] ActivateAsync failed: {task.Exception?.Message}")
                                    , TaskContinuationOptions.OnlyOnFaulted);
    }

    public void SetCurrent(string name)
    {
        var found = _personalities.FirstOrDefault(personality => personality.Name == name);

        if (found == null) return;

        Current = found;
        ApplyModelConfig(Current);

        _ = _apiClient?.ActivateAsync(found.Id)
                        .ContinueWith(task => Debug.WriteLine($"[PersonalityService] ActivateAsync failed: {task.Exception?.Message}")
                                    , TaskContinuationOptions.OnlyOnFaulted);
    }

    public List<Personality> GetAll() => _personalities;

    public Personality? FindBestMatch(string emotionOrContext)
    {
        return _personalities.FirstOrDefault(personality =>
               personality.Description != null
            && (personality.Description.Contains(emotionOrContext)
             || personality.Name.Contains(emotionOrContext)));
    }

    public void Add(Personality personality)
    {
        _personalities.Add(personality);
    }

    private void SetDefault(Personality? previous = null)
    {
        if (_personalities.Count == 0)
        {
            var fallback = new Personality
                           {
                                 Name         = "Friendly Helper"
                               , Description  = "Kind, casual, and helpful"
                               , SystemPrompt = "You're a helpful assistant. Be friendly and warm."
                               , IsDefault    = true
                           };

            _personalities.Add(fallback);
            Current = fallback;
            ApplyModelConfig(Current);
            return;
        }

        if (previous != null)
        {
            var preserved = _personalities.FirstOrDefault(personality => personality.Id == previous.Id);

            if (preserved != null)
            {
                Current = preserved;
                ApplyModelConfig(Current);
                return;
            }
        }

        Current = _personalities.FirstOrDefault(personality => personality.IsDefault)
               ?? _personalities.First();

        ApplyModelConfig(Current);
    }

    private void ApplyModelConfig(Personality personality)
    {
        if (personality.ModelConfig == null)
            return;

        _ollamaConfigService.UpdateConfig(ToOllamaConfig(personality.ModelConfig));
    }

    private static OllamaConfig ToOllamaConfig(ModelConfig config)
    {
        return new OllamaConfig
               {
                     Host        = StringConsts.OllamaServerUrl
                   , Model       = config.Model       ?? "default-model"
                   , Temperature = config.Temperature ?? 0.7f
                   , NumPredict  = config.NumPredict  ?? 256
               };
    }

    private void ReloadAll()
    {
        var previous = Current;

        _personalities.Clear();

        foreach (var provider in _personalityProviders)
            _personalities.AddRange(provider.Load());

        SetDefault(previous);
    }
}
