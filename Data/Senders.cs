namespace LocalAIAssistant.Data;

public static class Senders
{
    public const string User      = nameof(User);
    public const string Assistant = nameof(Assistant);
    public const string System    = nameof(System);
    public const string Memory    = nameof(Memory);
    
    public const string UserLowered      = "user";
    public const string AiLowered        = "ai";
    public const string AssistantLowered = "assistant";
    public const string SystemLowered    = "system";
    public const string MemoryLowered    = "memory";
    
    public static readonly string Ai      = nameof(Ai).ToUpper();
    public static readonly string Unknown = nameof(Unknown).ToUpper();

}