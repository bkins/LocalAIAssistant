using CP.Shared.Primitives.Avails.Extensions;

namespace LocalAIAssistant.Knowledge.Inbox;

/// <summary>Contains navigation failures without changing the Inbox's existing detail routes.</summary>
public static class KnowledgeItemDetailNavigation
{
    public static async Task<string?> OpenAsync(KnowledgeItem item, Func<string, Task> navigate)
    {
        if (item.IsQueued || item.Kind == KnowledgeKind.Pending)
        {
            return null;
        }

        var route = item.Kind switch
        {
            KnowledgeKind.Journal => "JournalDetailPage"
          , KnowledgeKind.Task    => "TaskDetailPage"
          , _                     => null
        };

        if (route is null)
        {
            return $"Opening {item.Kind} entries from the Inbox is not supported yet.";
        }

        var url = $"{route}?id={item.Id}";
        if (item.Workspace is { } workspace && workspace.HasValue())
        {
            url += $"&workspace={Uri.EscapeDataString(workspace)}";
        }

        try
        {
            await navigate(url);
            return null;
        }
        catch (Exception exception)
        {
            Serilog.Log.Error(exception, "Failed to open Inbox item {ItemId} of kind {Kind}", item.Id, item.Kind);
            return "Unable to open this entry. Try again; if it continues, check the app logs.";
        }
    }
}
