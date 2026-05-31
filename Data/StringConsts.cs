namespace LocalAIAssistant.Data;

public static class StringConsts
{
    public const string OllamaServerIpAddress = "192.168.0.33";
    public const string OllamaServerPort      = "11434";
    public const string OllamaServerUrl       = @$"http://{OllamaServerIpAddress}:{OllamaServerPort}";
    
    public const string AiMemoryTableName        = "AiMemory";
    public const string OllamaChatEndpoint       = "api/chat";
    public const string OllamaGenerateEndpoint   = "api/generate";
    
    public const string OllamaConfigJsonFileName   = "OllamaConfig.json";
    public const string PersonalitiesFileName      = "Personalities.json";
    public const string PersonalitiesLocalFileName = "Personalities.local.json";
    public const string SelectedPersonalityPrefKey  = "SelectedPersonalityName";
    public const string ActiveConversationIdKey     = "ActiveConversationId";

    public const string TtsEnabledPrefKey            = "TtsEnabled";
    public const string TtsPreferredVoiceNamePrefKey = "TtsPreferredVoiceName";
    public const string TtsProviderPrefKey           = "TtsProvider";
    public const string TtsAzureKeyPrefKey           = "TtsAzureKey";
    public const string TtsAzureRegionPrefKey        = "TtsAzureRegion";
    public const string TtsElevenLabsKeyPrefKey      = "TtsElevenLabsKey";

    public static string ApplicationJsonMediaType = "application/json";

    // Grog key: stored in: %APPDATA%\Microsoft\UserSecrets\

}