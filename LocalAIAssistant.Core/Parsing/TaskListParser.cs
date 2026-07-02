using System.Text.RegularExpressions;
using CP.Client.Core.Avails;

namespace LocalAIAssistant.Core.Parsing;

public static class TaskListParser
{
    private static readonly Regex TaskLineRegex = new(RegexMatchingPatterns.StructuredTaskLinePattern
                                                    , RegexOptions.Compiled 
                                                    | RegexOptions.IgnoreCase);

    public static bool TryParseTasks(string text, out List<ParsedTask> tasks)
    {
        tasks = new();

        if (text.HasNoValue()) return false;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = TaskLineRegex.Match(line.Trim());
            if (match.Success.Not())
                continue;

            var id     = match.Groups["id"].Value;
            var title  = match.Groups["title"].Value;
            var status = match.Groups["status"].Value;
            var tags   = match.Groups["tags"].Success
                                 ? match.Groups["tags"].Value.Split(',', StringSplitOptions.TrimEntries)
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
