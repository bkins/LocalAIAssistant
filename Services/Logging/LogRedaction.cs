using System.Text.RegularExpressions;

namespace LocalAIAssistant.Services.Logging;

public static partial class LogRedaction
{
    private static readonly string[] SensitiveKeys = ["authorization", "apikey", "api_key", "secret", "password", "token"];

    public static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return BearerPattern().Replace(value, "$1[REDACTED]");
    }

    public static IEnumerable<KeyValuePair<string, string>> Properties(IEnumerable<KeyValuePair<string, string>> properties)
    {
        foreach (var property in properties)
        {
            var sensitive = SensitiveKeys.Any(key => property.Key.Contains(key, StringComparison.OrdinalIgnoreCase));
            yield return new KeyValuePair<string, string>(property.Key, sensitive ? "[REDACTED]" : Text(property.Value));
        }
    }

    [GeneratedRegex("(?i)(bearer\\s+)[A-Za-z0-9._~+/-]+")]
    private static partial Regex BearerPattern();
}
