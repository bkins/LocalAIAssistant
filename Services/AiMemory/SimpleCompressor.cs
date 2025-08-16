using System.Text.RegularExpressions;
using LocalAIAssistant.Data.Models;

namespace LocalAIAssistant.Services.AiMemory;

public static class SimpleCompressor
{
    public static string BuildSummary(IEnumerable<Message> stm, IEnumerable<Message> ltm, int maxChars, bool includeTs)
    {
        var lines = new List<string>();

        var ltmList = ltm?.ToList() ?? new();
        var stmList = stm?.ToList() ?? new();

        if (ltmList.Count > 0) {
            lines.Add("Long-term facts:");
            foreach (var m in ltmList) lines.Add(Bullet(m, includeTs));
        }
        if (stmList.Count > 0) {
            lines.Add("Recent events:");
            foreach (var m in stmList) lines.Add(Bullet(m, includeTs));
        }

        var summary = string.Join("\n", lines);

        if (summary.Length <= maxChars) return summary;

        // prune from the bottom (oldest details first), keep headers
        for (int i = lines.Count - 1; i >= 0 && summary.Length > maxChars; i--)
        {
            if (lines[i].EndsWith(":")) continue; // keep section headers
            lines.RemoveAt(i);
            summary = string.Join("\n", lines);
        }

        // last resort
        return summary.Length <= maxChars ? summary : summary[..(maxChars - 3)] + "...";
    }

    private static string Bullet(Message m, bool includeTs)
    {
        var who = string.IsNullOrWhiteSpace(m.Sender) ? "User/AI" : m.Sender;
        var content = OneLine(m.Content, 220);
        var ts = includeTs ? $" [{m.Timestamp:yyyy-MM-dd HH:mm}]" : "";
        return $"- {who}{ts}: {content}";
    }

    private static string OneLine(string s, int limit)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var clean = Regex.Replace(s.Replace("\r", " ").Replace("\n", " "), @"\s+", " ").Trim();
        return clean.Length <= limit ? clean : clean[..(limit - 1)] + "…";
    }
}
