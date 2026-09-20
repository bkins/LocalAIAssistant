namespace LocalAIAssistant.Core.Versioning;

public static class RunningApplicationVersion
{
    public static string Format(string? displayVersion, string? buildVersion)
    {
        var display = displayVersion?.Trim() ?? string.Empty;
        var build   = buildVersion?.Trim() ?? string.Empty;

        if (display.Length == 0)
            return build.Length == 0 ? "Version unavailable" : $"v{build}";

        if (build.Length == 0 || display.EndsWith($".{build}", StringComparison.OrdinalIgnoreCase))
            return $"v{display}";

        return $"v{display}.{build}";
    }
}
