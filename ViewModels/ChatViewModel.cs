using LocalAIAssistant.Services.Logging;

namespace LocalAIAssistant.ViewModels;

public class ChatViewModel
{

    private readonly ILoggingService _logger;

    public ChatViewModel(ILoggingService logger)
    {
        _logger = logger;
    }

    public void SendMessage(string message)
    {
        // _logger.Log($"User: {message}");
        // // Send to AI...
        // var aiResponse = "AI says hello!";
        // _logger.Log($"AI: {aiResponse}");
    }

    public string BuildAiContext()
    {
        // var allLogs = _logger.GetLongTermMemory();
        // return string.Join("\n", allLogs.Select(l => $"[{l.Timestamp:O}] {l.Message}"));
        return "not implemented";
    }

}