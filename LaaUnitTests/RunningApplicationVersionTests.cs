using LocalAIAssistant.Core.Versioning;

namespace LaaUnitTests;

public sealed class RunningApplicationVersionTests
{
    [Theory]
    [InlineData("1.3.0", "111", "v1.3.0.111")]
    [InlineData("1.3.0.111", "111", "v1.3.0.111")]
    [InlineData("1.3.0", "", "v1.3.0")]
    [InlineData("", "111", "v111")]
    [InlineData("", "", "Version unavailable")]
    public void Format_UsesExactRunningVersionWithoutDuplicatingBuild( string displayVersion
                                                                    , string buildVersion
                                                                    , string expected)
    {
        var result = RunningApplicationVersion.Format(displayVersion, buildVersion);

        Assert.Equal(expected, result);
    }
}
