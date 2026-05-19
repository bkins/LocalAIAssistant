using LocalAIAssistant.Core.Personalities;
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
        return new Personality
               {
                     Id                = record.Id
                   , Name              = record.Name
                   , Description       = record.Description
                   , SystemPrompt      = record.SystemPrompt ?? string.Empty
                   , IsDefault         = record.IsDefault
                   , Tone              = record.Tone
                   , UseCase           = record.UseCase
                   , VoiceId           = record.VoiceId
                   , Tags              = record.Tags
                   , ModelConfig       = record.ModelConfig == null
                                             ? null
                                             : new ModelConfig
                                               {
                                                     Model       = record.ModelConfig.Model
                                                   , Temperature = record.ModelConfig.Temperature
                                                   , NumPredict  = record.ModelConfig.NumPredict
                                               }
               };
    }

    public void Dispose() => _watcher.Dispose();
}
