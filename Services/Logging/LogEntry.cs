using System.Text.Json;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services.Logging;

public class LogEntry
{
    public int      Id                 { get; set; }
    public DateTime Timestamp          { get; set; }
    public string   Level              { get; set; } = string.Empty;
    public string   Message            { get; set; } = string.Empty;
    public string   RenderedMessage    { get; set; } = string.Empty;
    public string   FullText           { get; set; } = string.Empty;
    public string   PrettifiedFullText => Prettify();
    public string   DisplayText        => $"[{Level}] {Message}";
    public string   FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    public string Prettify() => Prettify(FullText);

    public static string Prettify(string json)
    {
        if (json.HasNoValue()) return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc,
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
