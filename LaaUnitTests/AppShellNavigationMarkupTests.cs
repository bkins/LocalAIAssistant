namespace LaaUnitTests;

public sealed class AppShellNavigationMarkupTests
{
    [Fact]
    public void PrimaryTabs_UseApprovedOrderAndIcons()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AppShell.xaml.txt"));

        var chatIndex    = markup.IndexOf("<Tab Title=\"Chat\" Icon=\"tab_chat.svg\" AutomationId=\"ChatTabItem\">", StringComparison.Ordinal);
        var inboxIndex   = markup.IndexOf("<Tab Title=\"Inbox\" Icon=\"tab_inbox.svg\" AutomationId=\"InboxTabItem\">", StringComparison.Ordinal);
        var recordIndex  = markup.IndexOf("<Tab Title=\"Record\" Icon=\"tab_record.svg\" AutomationId=\"RecordTabItem\">", StringComparison.Ordinal);
        var memoryIndex  = markup.IndexOf("<Tab Title=\"Memory\" Icon=\"tab_psychology_alt.svg\"", StringComparison.Ordinal);
        var actionsIndex = markup.IndexOf("<Tab Title=\"Actions\" Icon=\"tab_actions.svg\" AutomationId=\"ActionsTabItem\">", StringComparison.Ordinal);

        Assert.True(chatIndex >= 0);
        Assert.True(inboxIndex > chatIndex);
        Assert.True(recordIndex > inboxIndex);
        Assert.True(memoryIndex > recordIndex);
        Assert.True(actionsIndex > memoryIndex);
    }

    [Fact]
    public void AuxiliaryTabs_RemainAfterPrimaryTabs()
    {
        var markup = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AppShell.xaml.txt"));

        var actionsIndex  = markup.IndexOf("AutomationId=\"ActionsTabItem\"", StringComparison.Ordinal);
        var chatsIndex    = markup.IndexOf("AutomationId=\"ChatsTabItem\"", StringComparison.Ordinal);
        var agentIndex    = markup.IndexOf("AutomationId=\"AgentTabItem\"", StringComparison.Ordinal);
        var logsIndex     = markup.IndexOf("AutomationId=\"LogsTabItem\"", StringComparison.Ordinal);
        var settingsIndex = markup.IndexOf("AutomationId=\"SettingsTabItem\"", StringComparison.Ordinal);

        Assert.True(chatsIndex > actionsIndex);
        Assert.True(agentIndex > actionsIndex);
        Assert.True(logsIndex > actionsIndex);
        Assert.True(settingsIndex > actionsIndex);
    }
}
