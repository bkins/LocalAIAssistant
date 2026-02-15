namespace LocalAIAssistant.Core.Environment.Models;

public class SystemEnvironmentInfo
{
    public string   EnvironmentName { get; init; } = default!;
    public string   MachineName     { get; init; } = default!;
    public string   ContentRoot     { get; init; } = default!;
    public string   DataRoot        { get; init; } = default!;
    public string   DatabasePath    { get; init; } = default!;
    public int      ProcessId       { get; init; }
    public DateTime StartedAtUtc    { get; init; }
}