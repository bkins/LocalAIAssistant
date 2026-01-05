namespace LocalAIAssistant.Services.Contracts;

public class OllamaRequest
{
    public string  Model   { get; set; } = string.Empty;
    public string  Prompt  { get; set; } = string.Empty;
}
