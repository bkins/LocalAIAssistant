using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalAIAssistant.Data.Models;

public partial class Message : ObservableObject
{
    [ObservableProperty]
    private int _id;
    
    [ObservableProperty]
    private string _sender;

    [ObservableProperty]
    private string _content;
    
    [ObservableProperty]
    private DateTime _timestamp;
    
    public Message() { }
    
    public Message(int id
                 , string sender
                 , string content
                 , DateTime timestamp)
    {
        Id        = id;
        Sender    = sender;
        Content   = content;
        Timestamp = timestamp;
    }
}
