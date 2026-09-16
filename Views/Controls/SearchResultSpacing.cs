using System.Text.RegularExpressions;

namespace LocalAIAssistant.Views.Controls;

public static class SearchResultSpacing
{
    private static readonly Regex ResultHeader = new(@"^\[[a-zA-Z]+\] [^\r\n]+ \(score: -?\d+(?:[.,]\d+)?\)(?:\r?\n|$)", RegexOptions.CultureInvariant);

    public static double TopGap(string text)
        => ResultHeader.IsMatch(text) ? 12 : 0;
}
