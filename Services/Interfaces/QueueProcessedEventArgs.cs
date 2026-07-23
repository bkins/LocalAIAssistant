namespace LocalAIAssistant.Services.Interfaces;

public class QueueProcessedEventArgs : EventArgs
{
    public int ReplayedCount { get; }

    public QueueProcessedEventArgs(int replayedCount)
    {
        ReplayedCount = replayedCount;
    }
}
