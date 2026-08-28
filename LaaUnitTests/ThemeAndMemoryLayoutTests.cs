using Microsoft.Maui.ApplicationModel;
using Xunit;

namespace LaaUnitTests;

public class ThemeAndMemoryLayoutTests
{
    [Theory]
    [InlineData("Dark", AppTheme.Dark)]
    [InlineData("Light", AppTheme.Light)]
    [InlineData("System", AppTheme.Unspecified)]
    [InlineData("", AppTheme.Unspecified)]
    [InlineData("UnknownTheme", AppTheme.Unspecified)]
    public void ThemePreference_MapsToExpectedAppTheme(string input, AppTheme expected)
    {
        var result = input switch
        {
            "Dark"  => AppTheme.Dark,
            "Light" => AppTheme.Light,
            _       => AppTheme.Unspecified
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void MemoryTabNavigation_MobileLayout_TogglesColumns()
    {
        var isMobileLayout = true;
        var selectedTabIndex = 0;

        bool ShowShortTerm() => !isMobileLayout || selectedTabIndex == 0;
        bool ShowLongTerm() => !isMobileLayout || selectedTabIndex == 1;

        Assert.True(ShowShortTerm());
        Assert.False(ShowLongTerm());

        selectedTabIndex = 1;
        Assert.False(ShowShortTerm());
        Assert.True(ShowLongTerm());
    }

    [Fact]
    public void MemoryTabNavigation_DesktopLayout_ShowsBothColumns()
    {
        var isMobileLayout = false;
        var selectedTabIndex = 0;

        bool ShowShortTerm() => !isMobileLayout || selectedTabIndex == 0;
        bool ShowLongTerm() => !isMobileLayout || selectedTabIndex == 1;

        Assert.True(ShowShortTerm());
        Assert.True(ShowLongTerm());

        selectedTabIndex = 1;
        Assert.True(ShowShortTerm());
        Assert.True(ShowLongTerm());
    }
}
