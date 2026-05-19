using System.Text.Json;

namespace LocalAIAssistant.Core.Personalities;

public class PersonalityCatalogLoader : IPersonalityCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    public PersonalityCatalogLoader(string filePath)
    {
        _filePath = filePath;
    }

    public List<PersonalityRecord> Load()
    {
        if (!File.Exists(_filePath))
            return new List<PersonalityRecord>();

        var json    = File.ReadAllText(_filePath);
        var wrapper = JsonSerializer.Deserialize<PersonalityRecordFile>(json, JsonOptions);

        return wrapper?.Personalities ?? new List<PersonalityRecord>();
    }
}
