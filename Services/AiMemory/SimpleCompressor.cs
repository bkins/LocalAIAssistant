using System.Text;
using System.Text.RegularExpressions;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Extensions;

namespace LocalAIAssistant.Services.AiMemory;

public static class SimpleCompressor
{

    public static string BuildSummary(IEnumerable<Message> shortTermMemory
                                    , IEnumerable<Message> longTermMemory
                                    , int                  maxChars
                                    , bool                 includeTimestamps
                                    , int                  maxStmItems   = 6
                                    , int                  maxLtmItems   = 6
                                    , int                  maxLineLength = 220)
    {
        var lines = new List<string>();

        AddLines(longTermMemory
               , includeTimestamps
               , maxLtmItems
               , maxLineLength
               , lines
               , "Long-term facts:");

        var shortTermMemoryList = shortTermMemory.ToList();
        AddLines(shortTermMemoryList
               , includeTimestamps
               , maxStmItems
               , maxLineLength
               , lines
               , "Recent conversation:");

        var summary = string.Join("\n"
                                , lines);

        if (summary.Length <= maxChars) return summary;

        for (int i = lines.Count - 1; i >= 0 && summary.Length > maxChars; i--)
        {
            if (lines[i].EndsWith(':')) continue;
            lines.RemoveAt(i);
            summary = string.Join("\n"
                                , lines);
        }

        if (summary.Length > maxChars)
            summary = summary[..(maxChars - 3)] + "...";

        return summary;
    }

    private static void AddLines(IEnumerable<Message> memoryList
                               , bool                 includeTimestamps
                               , int                  maxItems
                               , int                  maxLineLength
                               , List<string>         lines
                               , string               sectionTitle)
    {
        var list = memoryList?
                   .OrderByDescending(m => m.Importance)
                   .ThenByDescending(m => m.Score)
                   .ThenByDescending(m => m.Timestamp)
                   .Take(maxItems)
                   .ToList() ?? new List<Message>();

        if (list.Count > 0)
        {
            lines.Add(sectionTitle);
            foreach (var message in list)
                lines.Add(Bullet(message
                               , includeTimestamps
                               , maxLineLength
                               , message.Score));
        }
    }

    private static string Bullet(Message message
                               , bool    includeTimestamp
                               , int     maxLineLength
                               , double? score)
    {
        var who            = message.Sender.HasNoValue() ? Senders.Unknown : message.Sender;
        var content        = OneLine(message.Content, maxLineLength);
        var timestamp      = includeTimestamp ? $" [{message.Timestamp:yyyy-MM-dd HH:mm}]" : "";
        var scoreText      = $" (Score: {score:0.##})";
        var tagsText       = message.Tags?.Any() == true ? $" [Tags: {string.Join(",", message.Tags)}]" : "";
        var importanceText = message.Importance > 1 ? $" [Imp: {message.Importance}]" : "";

        return $"- {who}{timestamp}{tagsText}{importanceText}{scoreText}: {content}";
    }

    private static string OneLine(string messageContent
                                , int    limit)
    {
        if (string.IsNullOrWhiteSpace(messageContent)) return "";
        var clean = Regex.Replace(messageContent.Replace("\r"
                                                       , " ").Replace("\n"
                                                                    , " ")
                                , @"\s+"
                                , " ").Trim();

        return clean.Length <= limit ? clean : clean[..(limit - 1)] + "…";
    }

}
