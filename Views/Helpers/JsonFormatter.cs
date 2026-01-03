using System.Text.Json;

namespace LocalAIAssistant.Views.Helpers;

public static class JsonFormatter
{
    public static string Prettify(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions
                                                 {
                                                     WriteIndented = true
                                                 });
        }
        catch (JsonException)
        {
            // Not valid JSON, return original
            return json;
        }
    }
}