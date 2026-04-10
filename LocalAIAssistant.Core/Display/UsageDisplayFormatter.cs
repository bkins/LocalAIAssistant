namespace LocalAIAssistant.Core.Display;

public static class UsageDisplayFormatter
{
    public static string FormatHeaderSummary( int requestsRemaining
                                            , int requestsLimit
                                            , int tokensRemaining
                                            , int tokensLimit )
    {
        var tokLabel = tokensRemaining >= 1000
                               ? $"{tokensRemaining / 1000.0:0.#}k"
                               : tokensRemaining.ToString();
        var tokLabelLimit = tokensLimit >= 1000
                               ? $"{tokensLimit / 1000.0:0.#}k"
                               : tokensLimit.ToString();

        return $"{requestsRemaining}/{requestsLimit} req · {tokLabel}/{tokLabelLimit} tok";
    }

    /// <summary>
    /// Returns "Gray", "Orange", or "Red" based on worst-case usage percentage.
    /// Keeps the threshold logic testable without a dependency on MAUI's Color type.
    /// </summary>
    public static string GetColorCategory( double requestPercent
                                         , double tokenPercent
                                         , double warnThreshold   = 70.0
                                         , double dangerThreshold = 90.0 )
    {
        var worst = Math.Max(requestPercent, tokenPercent);

        if (worst >= dangerThreshold) return "Red";
        if (worst >= warnThreshold)   return "Orange";

        return "Gray";
    }
}
