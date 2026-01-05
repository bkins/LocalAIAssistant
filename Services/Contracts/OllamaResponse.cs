namespace LocalAIAssistant.Services.Contracts;

public class OllamaResponse
{
    public OllamaMessage? Message { get; set; }
}

public class OllamaMessage
{
    public string? Role    { get; set; }
    public string? Content { get; set; }
}
