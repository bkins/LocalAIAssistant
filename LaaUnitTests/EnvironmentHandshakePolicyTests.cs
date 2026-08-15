using LocalAIAssistant.Core.Environment;
using LocalAIAssistant.Core.Environment.Models;
using Xunit;

namespace LaaUnitTests;

public class EnvironmentHandshakePolicyTests
{
    [Fact]
    public void Evaluate_MatchingEnvironments_ReturnsNoneSeverityAndAllowsWrites()
    {
        var result = EnvironmentHandshakePolicy.Evaluate("DEV", "DEV");

        Assert.Equal(HandshakeSeverity.None, result.Severity);
        Assert.True(result.AllowWrites);
    }

    [Fact]
    public void Evaluate_ClientProdMismatch_ReturnsRestrictedSeverityAndDisallowsWrites()
    {
        var result = EnvironmentHandshakePolicy.Evaluate("PROD", "DEV");

        Assert.Equal(HandshakeSeverity.Restricted, result.Severity);
        Assert.False(result.AllowWrites);
    }

    [Fact]
    public void Evaluate_ApiProdMismatch_ReturnsRestrictedSeverityAndDisallowsWrites()
    {
        var result = EnvironmentHandshakePolicy.Evaluate("DEV", "PROD");

        Assert.Equal(HandshakeSeverity.Restricted, result.Severity);
        Assert.False(result.AllowWrites);
    }

    [Fact]
    public void Evaluate_QaMismatch_ReturnsWarningSeverityAndAllowsWrites()
    {
        var result = EnvironmentHandshakePolicy.Evaluate("QA", "DEV");

        Assert.Equal(HandshakeSeverity.Warning, result.Severity);
        Assert.True(result.AllowWrites);
    }

    [Fact]
    public void Evaluate_DevMismatch_ReturnsInfoSeverityAndAllowsWrites()
    {
        var result = EnvironmentHandshakePolicy.Evaluate("DEV", "STAGING");

        Assert.Equal(HandshakeSeverity.Info, result.Severity);
        Assert.True(result.AllowWrites);
    }

    [Fact]
    public void Failed_WithException_ReturnsErrorSeverityAndDisallowsWrites()
    {
        var ex = new HttpRequestException("Connection refused");

        var result = EnvironmentHandshakePolicy.Failed("DEV", "DEV", ex);

        Assert.Equal(HandshakeSeverity.Error, result.Severity);
        Assert.False(result.AllowWrites);
        Assert.Contains("Connection refused", result.UserMessage);
    }
}
