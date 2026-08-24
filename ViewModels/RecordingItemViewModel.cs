using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalAIAssistant.Core.ConversationRecorder;
using LocalAIAssistant.Services.Recordings;

namespace LocalAIAssistant.ViewModels;

public partial class TranscriptSegmentViewModel : ObservableObject
{
    [ObservableProperty] private string _speakerLabel = string.Empty;
    [ObservableProperty] private string _timeRangeDisplay = string.Empty;
    [ObservableProperty] private string _text = string.Empty;

    public TranscriptSegmentViewModel(TranscriptSegmentDto dto, Dictionary<string, string>? speakerMap = null)
    {
        var rawLabel = dto.SpeakerLabel ?? dto.SpeakerId ?? "Speaker 1";
        if (speakerMap != null && speakerMap.TryGetValue(rawLabel, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
        {
            SpeakerLabel = mapped;
        }
        else
        {
            SpeakerLabel = rawLabel;
        }

        var startStr = dto.Start.ToString(@"mm\:ss");
        var endStr = dto.End.ToString(@"mm\:ss");
        TimeRangeDisplay = $"{startStr} - {endStr}";
        Text = dto.Text;
    }
}

public partial class RecordingItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _startedAtDisplay = string.Empty;
    [ObservableProperty] private string _durationDisplay = string.Empty;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private string _recordingPath = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isPlaybackPaused;
    [ObservableProperty] private bool _isTranscribing;
    [ObservableProperty] private bool _isTranscriptExpanded;
    [ObservableProperty] private string _speakerName1 = "Speaker 1";
    [ObservableProperty] private string _speakerName2 = "Speaker 2";
    [ObservableProperty] private TranscriptDto? _transcript;

    public ObservableCollection<TranscriptSegmentViewModel> Segments { get; } = new();

    public bool HasTranscript => Transcript != null && Transcript.Segments.Count > 0;

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

    public void LoadTranscript(TranscriptDto? transcript, List<ConversationParticipantDto>? participants = null)
    {
        Transcript = transcript;
        if (transcript != null)
        {
            Status = transcript.IsDiarized ? "Diarized" : "Transcribed";
        }

        var speakerMap = new Dictionary<string, string>();
        if (participants != null && participants.Count > 0)
        {
            foreach (var p in participants)
            {
                speakerMap[p.SpeakerId] = p.DisplayName;
                if (p.SpeakerId.Equals("Speaker 1", StringComparison.OrdinalIgnoreCase))
                {
                    SpeakerName1 = p.DisplayName;
                }
                else if (p.SpeakerId.Equals("Speaker 2", StringComparison.OrdinalIgnoreCase))
                {
                    SpeakerName2 = p.DisplayName;
                }
            }
        }

        Segments.Clear();
        if (transcript != null)
        {
            foreach (var segment in transcript.Segments)
            {
                Segments.Add(new TranscriptSegmentViewModel(segment, speakerMap));
            }
        }

        OnPropertyChanged(nameof(HasTranscript));
    }
}
