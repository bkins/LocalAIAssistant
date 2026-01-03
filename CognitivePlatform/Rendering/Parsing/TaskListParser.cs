using System.Text.RegularExpressions;

namespace LocalAIAssistant.CognitivePlatform.Rendering.Parsing;

public static class TaskListParser
{
    private static readonly Regex TaskLineRegex = new(
        @"^- (?<id>[a-f0-9]+): (?<title>.+?) \[(?<status>[^]]+)\](?: \[tags: (?<tags>[^]]+)\])?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParseTasks(string text, out List<ParsedTask> tasks)
    {
        tasks = new();

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var m = TaskLineRegex.Match(line.Trim());
            if (!m.Success)
                continue;

            var id     = m.Groups["id"].Value;
            var title  = m.Groups["title"].Value;
            var status = m.Groups["status"].Value;
            var tags = m.Groups["tags"].Success
                               ? m.Groups["tags"].Value.Split(',', StringSplitOptions.TrimEntries)
                               : Array.Empty<string>();

            var importance = status.Contains("Important", StringComparison.OrdinalIgnoreCase);
            var urgent     = status.Contains("Urgent",    StringComparison.OrdinalIgnoreCase);

            tasks.Add(new ParsedTask
                      {
                              Id          = id,
                              Title       = title,
                              IsImportant = importance,
                              IsUrgent    = urgent,
                              Tags        = tags.ToList()
                      });
        }

        return tasks.Count > 0;
    }
}

public class ParsedTask
{
    public string       Id          { get; set; } = "";
    public string       Title       { get; set; } = "";
    public bool         IsImportant { get; set; }
    public bool         IsUrgent    { get; set; }
    public List<string> Tags        { get; set; } = new();
}