using System.Text.Json;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Personalities;

public class JsonPersonalityProvider : IPersonalityProvider, IDisposable
{
    private readonly string            _filePath;
    private readonly FileSystemWatcher _watcher;

    private List<Personality> _cache = new();

    public event Action? OnReload;

    public JsonPersonalityProvider(string filePath)
    {
        _filePath = filePath;

        LoadFromFile();

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
            // small delay to avoid file lock issues
            Thread.Sleep(50);

            LoadFromFile();
            OnReload?.Invoke();
        }
        catch
        {
            // swallow for now (or log)
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath))
        {
            _cache = new List<Personality>();
            return;
        }

        var json = File.ReadAllText(_filePath);

        var wrapper = JsonSerializer.Deserialize<PersonalityFile>(json);

        _cache = wrapper?.Personalities ?? new List<Personality>();
    }

    public void Dispose() => _watcher.Dispose();
}
