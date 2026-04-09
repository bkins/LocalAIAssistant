namespace LaaUnitTests;

// ─────────────────────────────────────────────────────────────────────────────
// Specification tests for UsageViewModel display-formatting helpers
//
// Production source:
//   LocalAIAssistant/ViewModels/UsageViewModel.cs
//     — FormatHeaderSummary (private static)
//     — DeriveHeaderColor   (private — logic mirrored here without MAUI Color type)
//
// The UnitTestsFrontend project targets net9.0 and cannot directly reference the
// MAUI project. These tests carry a local mirror of the two pure helper methods.
//
// BACKLOG-07: when these helpers are extracted to a shared net9.0 library, remove
// the local mirrors and replace with direct using references.
// ─────────────────────────────────────────────────────────────────────────────

public class UsageDisplayFormatterTests
{
    // ── FormatHeaderSummary ───────────────────────────────────────────────────

    [Theory]
    [InlineData(847, 1000, 42000, 100000, "847/1000 req · 42k/100k tok")]
    [InlineData(500, 1000,   999,   1000, "500/1000 req · 999/1k tok")]     // remaining < 1000 (no k), limit == 1000 (formats as 1k)
    [InlineData(  0,  100,     0,    100, "0/100 req · 0/100 tok")]          // zeros
    [InlineData( 10,   10,  1500,   2000, "10/10 req · 1.5k/2k tok")]        // fractional k
    [InlineData(200,  500, 50000, 100000, "200/500 req · 50k/100k tok")]     // round thousands
    public void FormatHeaderSummary_ProducesExpectedLabel( int    requestsRemaining
                                                         , int    requestsLimit
                                                         , int    tokensRemaining
                                                         , int    tokensLimit
                                                         , string expected )
    {
        var result = LocalUsageDisplayFormatter.FormatHeaderSummary(requestsRemaining
                                                                   , requestsLimit
                                                                   , tokensRemaining
                                                                   , tokensLimit);

        Assert.Equal(expected, result);
    }

    // ── DeriveHeaderColor (threshold logic only — no MAUI Color type) ─────────

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
        var result = LocalUsageDisplayFormatter.GetColorCategory(requestPercent
                                                                , tokenPercent
                                                                , warnThreshold:   70.0
                                                                , dangerThreshold: 90.0);

        Assert.Equal(expectedCategory, result);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Local mirror of production display helpers
// Keep in sync with:
//   LocalAIAssistant/ViewModels/UsageViewModel.cs
//     — FormatHeaderSummary  (private static)
//     — DeriveHeaderColor    (private, mapped to string category here)
// ─────────────────────────────────────────────────────────────────────────────

internal static class LocalUsageDisplayFormatter
{
    /// <summary>
    /// Mirrors <c>UsageViewModel.FormatHeaderSummary</c>.
    /// Returns a compact header label such as "847/1000 req · 42k/100k tok".
    /// </summary>
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
    /// Mirrors the threshold logic in <c>UsageViewModel.DeriveHeaderColor</c>.
    /// Returns a string category ("Gray" / "Orange" / "Red") so the test project
    /// does not need to reference MAUI's <c>Color</c> type.
    /// </summary>
    public static string GetColorCategory( double requestPercent
                                         , double tokenPercent
                                         , double warnThreshold
                                         , double dangerThreshold )
    {
        var worst = Math.Max(requestPercent, tokenPercent);

        if (worst >= dangerThreshold) return "Red";
        if (worst >= warnThreshold)   return "Orange";

        return "Gray";
    }
}
