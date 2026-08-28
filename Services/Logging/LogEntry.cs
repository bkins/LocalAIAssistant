using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services.Logging;

public class LogEntry
{
    public int                       Id                 { get; set; }
    public DateTime                  Timestamp          { get; set; }
    public string                    Level              { get; set; } = "Information";
    public string                    Category           { get; set; } = "General";
    public string                    Message            { get; set; } = string.Empty;
    public string                    RenderedMessage    { get; set; } = string.Empty;
    public string?                   Exception          { get; set; }
    public Dictionary<string, string> Properties         { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string                    FullText           { get; set; } = string.Empty;

    public bool HasException => Exception.HasValue();

    public string LevelBadgeText => Level.ToUpperInvariant() switch
    {
        "ERROR" or "CRITICAL" or "FATAL" => "ERR",
        "WARNING" or "WARN"              => "WRN",
        "INFORMATION" or "INFO"          => "INF",
        "DEBUG" or "DBG"                 => "DBG",
        "TRACE" or "TRC"                 => "TRC",
        _                                => Level.Length > 3 ? Level[..3].ToUpperInvariant() : Level.ToUpperInvariant()
    };

    public string FormattedTime      => Timestamp.ToString("HH:mm:ss.fff");
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string DisplayText        => $"[{FormattedTime}] [{LevelBadgeText}] [{Category}] {Message}";
    public string PrettifiedFullText => Prettify(FullText);

    public string PropertiesFormatted
    {
        get
        {
            if (Properties.Count == 0) return "None";
            var sb = new StringBuilder();
            foreach (var kvp in Properties)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static string Prettify(string json)
    {
        if (json.HasNoValue()) return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
