namespace LocalAIAssistant;

public static class ApiConfig
{
    public static string OllamaBaseUrl
    {
        get
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return "http://192.168.0.33:11434/";
            }
            else
            {
                return "http://10.0.2.2:11434/api/chat";
            }
            
        }
    }

}