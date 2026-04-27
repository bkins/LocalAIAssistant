using LocalAIAssistant.Core.Display;
using Xunit;

namespace LaaUnitTests;

// ─────────────────────────────────────────────────────────────────────────────
// Specification tests for UsageDisplayFormatter
//
// Production source: LocalAIAssistant.Core/Display/UsageDisplayFormatter.cs
// ─────────────────────────────────────────────────────────────────────────────

public class UsageDisplayFormatterTests
{
    // ── FormatHeaderSummary ───────────────────────────────────────────────────

    [Theory]
    [InlineData(847, 1000, 42000, 100000, "847/1000 requests · 42k/100k tokens ")]
    [InlineData(500, 1000,   999,   1000, "500/1000 requests · 999/1k tokens ")]     // remaining < 1000 (no k), limit == 1000 (formats as 1k)
    [InlineData(  0,  100,     0,    100, "0/100 requests ()· 0/100 tokens ()")]          // zeros
    [InlineData( 10,   10,  1500,   2000, "10/10 requests · 1.5k/2k tokens ")]        // fractional k
    [InlineData(200,  500, 50000, 100000, "200/500 requests · 50k/100k tokens ")]     // round thousands
    public void FormatHeaderSummary_ProducesExpectedLabel( int    requestsRemaining
                                                         , int    requestsLimit
                                                         , int    tokensRemaining
                                                         , int    tokensLimit
                                                         , string expected )
    {
        //TODO :  Add tests for the reset label parameters once we have a better idea of how those will be used and formatted.
        // For now, just verify that passing empty strings doesn't break anything.
        var result = UsageDisplayFormatter.FormatHeaderSummary(requestsRemaining
                                                             , requestsLimit
                                                             , string.Empty
                                                             , tokensRemaining
                                                             , tokensLimit
                                                             , string.Empty);

        Assert.Equal(expected, result);
    }

    // ── GetColorCategory (threshold logic only — no MAUI Color type) ──────────

    [Theory]
    [InlineData(50.0,  50.0,  "Gray")]     // both below warn threshold
    [InlineData(69.9,  69.9,  "Gray")]     // just below warn
    [InlineData(70.0,   0.0,  "Orange")]   // requests at warn threshold
    [InlineData( 0.0,  70.0,  "Orange")]   // tokens at warn threshold
    [InlineData(75.0,  60.0,  "Orange")]   // requests in warn band
    [InlineData(89.9,  89.9,  "Orange")]   // both just below danger threshold
    [InlineData(90.0,   0.0,  "Red")]      // requests at danger threshold
    [InlineData( 0.0,  90.0,  "Red")]      // tokens at danger threshold
    [InlineData(95.0,  80.0,  "Red")]      // requests in danger band
    [InlineData(100.0, 100.0, "Red")]      // both fully depleted
    public void GetColorCategory_ReturnsExpectedCategory( double requestPercent
                                                        , double tokenPercent
                                                        , string expectedCategory )
    {
        var result = UsageDisplayFormatter.GetColorCategory(requestPercent
                                                           , tokenPercent
                                                           , warnThreshold:   70.0
                                                           , dangerThreshold: 90.0);

        Assert.Equal(expectedCategory, result);
    }
}
