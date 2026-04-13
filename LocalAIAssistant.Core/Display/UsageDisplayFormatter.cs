namespace LocalAIAssistant.Core.Display;

public static class UsageDisplayFormatter
{
    public static string FormatHeaderSummary( int    requestsRemaining
                                            , int    requestsLimit
                                            , string requestsResetLabel
                                            , int    tokensRemaining
                                            , int    tokensLimit
                                            , string tokensResetLabel )
    {
        var tokenLabel = tokensRemaining >= 1000
                               ? $"{tokensRemaining / 1000.0:0.#}k"
                               : tokensRemaining.ToString();
        var tokenLabelLimit = tokensLimit >= 1000
                               ? $"{tokensLimit / 1000.0:0.#}k"
                               : tokensLimit.ToString();

        var requestOverLimit = requestsRemaining <= 0;
        var tokenOverLimit   = tokensRemaining   <= 0;
        
        var requestReset = requestOverLimit 
                                   ? $"({requestsResetLabel})" : string.Empty;
        var tokenReset = tokenOverLimit
                                 ? $"({tokensResetLabel})"
                                 : string.Empty;
        
        var result = $"{requestsRemaining}/{requestsLimit} requests {requestReset}· {tokenLabel}/{tokenLabelLimit} tokens {tokenReset}";

        return result;
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
