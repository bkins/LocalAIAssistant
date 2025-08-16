namespace LocalAIAssistant.Services.Interfaces;

public interface ILlmService
{

    public IAsyncEnumerable<string> SendPromptStreamingAsync(string prompt);
    Task<bool>                      CheckApiHealthAsync();

}