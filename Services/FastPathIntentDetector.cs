using System.Text.RegularExpressions;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Services;

public static class FastPathIntentDetector
{
    private static readonly Regex PrefixRegex = new(RegexMatchingPatterns.NamedPayloadPattern, RegexOptions.Compiled);

    public static bool IsFastPathIntent(string? input)
    {
        var inputParts = input?.Split(new[] {':'}, 2, StringSplitOptions.RemoveEmptyEntries);
        
        return inputParts?.Length == 2
            && inputParts[0].Trim().Length > 0
            && inputParts[1].Trim().Length > 0;
        
        // return FastPathIntentDetector.TryGetPrefix(input, out string _, out string _);
    }

    public static bool TryGetPrefix(string? input, out string actionName, out string payload)
    {
        actionName = string.Empty;
        payload    = string.Empty;
        
        if ((input != null 
                     ? (input.HasNoValue() ? 1 : 0) 
                     : 1) != 0)
            return false;
        
        var match = PrefixRegex.Match(input);
        
        if (match.Success.Not()) return false;
        
        actionName = match.Groups["name"].Value;
        payload    = match.Groups[nameof (payload)].Value.Trim();
        
        return payload.Length > 0;
    }
}