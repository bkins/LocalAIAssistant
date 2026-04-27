using LocalAIAssistant.Core.Time;
using Xunit;

namespace LaaUnitTests;

// ─────────────────────────────────────────────────────────────────────────────
// Specification tests for TimestampConversion
//
// Production source: LocalAIAssistant.Core/Time/TimestampConversion.cs
// Covers UX-04: Memory page labels must show local time, not raw UTC.
// ─────────────────────────────────────────────────────────────────────────────

public class TimestampConversionTests
{
    // Pick an input timestamp whose local-vs-UTC difference is observable in any
    // non-UTC timezone. On a UTC server (offset == 0) the conversion is a no-op
    // and the local-zone assertions below still hold — Utc-kind input still
    // comes back as Local-kind after ToLocalTime().

    private static readonly DateTime UtcInstant = new(2026, 4, 24, 14, 30, 00, DateTimeKind.Utc);

    [Fact]
    public void ToLocalSafe_ConvertsUtc_ToLocal()
    {
        var expected = UtcInstant.ToLocalTime();

        var result = TimestampConversion.ToLocalSafe(UtcInstant);

        Assert.Equal(DateTimeKind.Local, result.Kind);
        Assert.Equal(expected,           result);
    }

    [Fact]
    public void ToLocalSafe_TreatsUnspecifiedAsUtc_AndConvertsToLocal()
    {
        var unspecified = DateTime.SpecifyKind(UtcInstant, DateTimeKind.Unspecified);
        var expected    = UtcInstant.ToLocalTime();

        var result = TimestampConversion.ToLocalSafe(unspecified);

        Assert.Equal(DateTimeKind.Local, result.Kind);
        Assert.Equal(expected,           result);
    }

    [Fact]
    public void ToLocalSafe_ReturnsLocalInput_Unchanged()
    {
        var local = new DateTime(2026, 4, 24, 9, 15, 00, DateTimeKind.Local);

        var result = TimestampConversion.ToLocalSafe(local);

        Assert.Equal(DateTimeKind.Local, result.Kind);
        Assert.Equal(local,              result);
    }
}
