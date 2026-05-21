using LocalAIAssistant.Core.Personalities;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Personalities;

public class JsonPersonalityProvider : IPersonalityProvider, IDisposable
{
    private readonly PersonalityCatalogLoader _loader;
    private readonly FileSystemWatcher        _watcher;

    private List<Personality> _cache = new();

    public event Action? OnReload;

    public JsonPersonalityProvider(string filePath)
    {
        _loader = new PersonalityCatalogLoader(filePath);

        RefreshCache();

        var directory = Path.GetDirectoryName(filePath)!;
        var file      = Path.GetFileName(filePath);

        _watcher = new FileSystemWatcher(directory, file)
                   {
                       NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                   };

        _watcher.Changed             += (_, _) => Reload();
        _watcher.EnableRaisingEvents =  true;
    }

    public IEnumerable<Personality> Load() => _cache;

    private void Reload()
    {
        try
        {
            Thread.Sleep(50);
            RefreshCache();
            OnReload?.Invoke();
        }
        catch
        {
            // swallow; a reload failure leaves the previous cache intact
        }
    }

    private void RefreshCache()
    {
        _cache = _loader.Load()
                        .Select(ToPersonality)
                        .ToList();
    }

    private static Personality ToPersonality(PersonalityRecord record)
    {
        ModelConfig? modelConfig = record.ModelConfig == null
            ? null
            : new ModelConfig
              {
                    Model       = record.ModelConfig.Model
                  , Temperature = record.ModelConfig.Temperature
                  , NumPredict  = record.ModelConfig.NumPredict
              };

        OllamaConfig? ollamaConfig = modelConfig == null
            ? null
            : new OllamaConfig
              {
                    Host        = StringConsts.OllamaServerUrl
                  , Model       = modelConfig.Model       ?? "default-model"
                  , Temperature = modelConfig.Temperature ?? 0.7f
                  , NumPredict  = modelConfig.NumPredict  ?? 256
              };

        return new Personality
               {
                     Id                 = record.Id
                   , Name               = record.Name
                   , Description        = record.Description
                   , SystemPrompt       = record.SystemPrompt ?? string.Empty
                   , IsDefault          = record.IsDefault
                   , Tone               = record.Tone
                   , UseCase            = record.UseCase
                   , VoiceId            = record.VoiceId
                   , Tags               = record.Tags
                   , ModelConfig        = modelConfig
                   , OllamConfiguration = ollamaConfig
               };
    }

    public void Dispose() => _watcher.Dispose();
}
