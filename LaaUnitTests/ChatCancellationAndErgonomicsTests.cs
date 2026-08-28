using CP.Client.Core.Avails;
using LocalAIAssistant.Data.Models;
using Xunit;

namespace LaaUnitTests;

public class ChatCancellationAndErgonomicsTests
{
    [Fact]
    public void Message_DisplayContent_StripsInsightPrefix_WhenIsInsightIsTrue()
    {
        var message = new Message
        {
            Sender = "assistant"
          , Content = "💡 Consider scheduling time to review tasks."
          , IsInsight = true
        };

        Assert.Equal("Consider scheduling time to review tasks.", message.DisplayContent);
        Assert.True(message.IsInsight);
    }

    [Fact]
    public void Message_FastPathAndInsight_PropertiesSetCorrectly()
    {
        var message = new Message
        {
            Sender = "assistant"
          , Content = "Added task."
          , WasFastPath = true
          , IsInsight = false
        };

        Assert.True(message.WasFastPath);
        Assert.False(message.IsInsight);
        Assert.Equal("Added task.", message.DisplayContent);
    }

    [Fact]
    public void Message_TelemetryText_IncludesFastPathSignal_WhenWasFastPathIsTrue()
    {
        var timestamp = new DateTime(2026, 8, 27, 10, 0, 0);
        var message = new Message
        {
            Sender = "assistant"
          , Content = "Done"
          , Timestamp = timestamp
          , WasFastPath = true
          , Provider = "RuleEngine"
          , Model = "FastPath"
        };

        var telemetry = message.TelemetryText;

        Assert.Contains("⚡ FastPath", telemetry);
    }

    [Fact]
    public void Message_TelemetryText_OmitsFastPathSignal_WhenWasFastPathIsFalse()
    {
        var timestamp = new DateTime(2026, 8, 27, 10, 0, 0);
        var message = new Message
        {
            Sender = "assistant"
          , Content = "Done"
          , Timestamp = timestamp
          , WasFastPath = false
          , Provider = "Groq"
          , Model = "llama-3.3-70b-versatile"
          , ResponseDurationMs = 450
        };

        var telemetry = message.TelemetryText;

        Assert.DoesNotContain("⚡ FastPath", telemetry);
        Assert.Contains("0.45s", telemetry);
    }
}
