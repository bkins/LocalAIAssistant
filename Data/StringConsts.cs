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

    // Coco (Code Intelligence) settings
    public const string CocoBaseUrlPrefKey                  = "CocoBaseUrl";
    public const string CocoDefaultBaseUrl                  = "http://localhost:5292";
    public const string CocoEnabledPrefKey                  = "CocoEnabled";
    public const string CocoProjectPathPrefKey              = "CocoProjectPath";
    public const string CocoClipboardMonitorEnabledPrefKey  = "CocoClipboardMonitorEnabled";
    public const string CocoHotkeyPrefKey                   = "CocoHotkey";
    public const string CocoDefaultHotkey                   = "Ctrl+Shift+C";

    // Grog key: stored in: %APPDATA%\Microsoft\UserSecrets\

}