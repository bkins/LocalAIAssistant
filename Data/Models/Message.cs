using CommunityToolkit.Mvvm.ComponentModel;
using LocalAIAssistant.CognitivePlatform.Rendering.Parsing;

namespace LocalAIAssistant.Data.Models;

public partial class Message : ObservableObject
{

    [ObservableProperty] private int          _id;
    [ObservableProperty] private string       _sender;
    [ObservableProperty] private string       _content;
    [ObservableProperty] private DateTime     _timestamp;
    [ObservableProperty] private string       _conversationId = "default";
    [ObservableProperty] private List<string> _tags           = new(); // e.g. ["summary","promotion"]
    [ObservableProperty] private int          _importance     = 1;     // 1..5
    [ObservableProperty] private double       _score;
    [ObservableProperty] private bool         _wasFastPath;

    public Message()
    {
    }

    public Message( int      id
                  , string   sender
                  , string   content
                  , DateTime timestamp
                  , string   conversationId
                  , int      score
                  , bool     wasFastPath )
    {
        Id             = id;
        Sender         = sender;
        Content        = content;
        Timestamp      = timestamp;
        ConversationId = conversationId;
        Score          = score;
        WasFastPath    = wasFastPath;
    }

    public bool IsMultiLine => Content?.Contains('\n') == true;
    public bool IsTaskList => 
            Sender?.Equals("assistant", StringComparison.OrdinalIgnoreCase) == true
         && TaskListParser.TryParseTasks(Content ?? "", out _);

    public List<ParsedTask>? ParsedTasks =>
            TaskListParser.TryParseTasks(Content ?? "", out var list) ? list : null;
}
