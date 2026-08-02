using LocalAIAssistant.CognitivePlatform.DTOs;
using LocalAIAssistant.Data.Models;
using Xunit;

namespace LaaUnitTests;

public class MessageTelemetryTests
{
    [Fact]
    public void HasTelemetry_ReturnsFalse_ForUserMessageWithNoDuration()
    {
        var msg = new Message
        {
            Sender = "user",
            Content = "Hello"
        };

        Assert.False(msg.HasTelemetry);
    }

    [Fact]
    public void HasTelemetry_ReturnsTrue_ForAssistantMessage()
    {
        var msg = new Message
        {
            Sender = "assistant",
            Content = "Hi there"
        };

        Assert.True(msg.HasTelemetry);
    }

    [Fact]
    public void TelemetryText_FormatsFastPathResponse_Correctly()
    {
        var timestamp = new DateTime(2026, 8, 2, 9, 30, 0);
        var msg = new Message
        {
            Sender = "assistant",
            WasFastPath = true,
            ResponseDurationMs = 250,
            Timestamp = timestamp
        };

        var text = msg.TelemetryText;

        Assert.Contains("⚡ FastPath (No LLM)", text);
        Assert.Contains("⏱ 0.25s", text);
        Assert.Contains(timestamp.ToString("g"), text);
    }

    [Fact]
    public void TelemetryText_FormatsLlmResponse_Correctly()
    {
        var timestamp = new DateTime(2026, 8, 2, 9, 35, 0);
        var msg = new Message
        {
            Sender = "assistant",
            WasFastPath = false,
            Provider = "Groq",
            Model = "llama-3.3-70b-versatile",
            ResponseDurationMs = 1450,
            Timestamp = timestamp
        };

        var text = msg.TelemetryText;

        Assert.Contains("🤖 Groq • llama-3.3-70b-versatile", text);
        Assert.Contains("⏱ 1.45s", text);
        Assert.Contains(timestamp.ToString("g"), text);
    }

    [Fact]
    public void ConverseResponseDto_MapsProviderAndModel()
    {
        var dto = new ConverseResponseDto
        {
            Message = "Test",
            Provider = "Ollama",
            Model = "phi-3:mini",
            WasFastPath = false
        };

        Assert.Equal("Ollama", dto.Provider);
        Assert.Equal("phi-3:mini", dto.Model);
        Assert.False(dto.WasFastPath);
    }
}
