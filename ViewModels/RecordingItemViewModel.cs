using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalAIAssistant.Services.Recordings;

namespace LocalAIAssistant.ViewModels;

public partial class RecordingItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _startedAtDisplay = string.Empty;
    [ObservableProperty] private string _durationDisplay = string.Empty;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private string _recordingPath = string.Empty;
    [ObservableProperty] private bool _isPlaying;

    public ConversationRecording Model { get; }

    public RecordingItemViewModel(ConversationRecording recording)
    {
        Model = recording ?? throw new ArgumentNullException(nameof(recording));
        Id = recording.Id;
        StartedAtDisplay = recording.StartedAt.ToLocalTime().ToString("g");
        DurationDisplay = recording.Duration.ToString(@"mm\:ss");
        Status = recording.Status;
        RecordingPath = recording.RecordingPath;
    }
}
