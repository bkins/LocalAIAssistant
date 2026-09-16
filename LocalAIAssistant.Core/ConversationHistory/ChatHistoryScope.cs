namespace LocalAIAssistant.Core.ConversationHistory;

public static class ChatHistoryScope
{
    public static string EnvironmentKey(string environment)
        => environment.Trim().ToLowerInvariant() switch
           {
               "dev" or "development" => "dev"
             , "debug" => "debug"
             , "qa" => "qa"
             , "prod" or "production" => "prod"
             , _ => throw new ArgumentException("Unknown chat environment.", nameof(environment))
           };

    public static string ActiveConversationKey(string environment)
        => $"Chat.{EnvironmentKey(environment)}.ActiveConversationId";

    public static string MemoryDirectory(string appDataDirectory, string environment)
        => Path.Combine(appDataDirectory, "Chat", EnvironmentKey(environment));
}
