using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Core.ConversationRecorder;
using LocalAIAssistant.Services.Recordings;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.ViewModels;

public partial class RecordingsViewModel : ObservableObject
{
    private readonly IConversationRecordingService _recordingService;
    private readonly IConversationRecorderApiClient? _recorderApiClient;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isPlaybackPaused;
    [ObservableProperty] private string _elapsedTimeDisplay = "00:00";
    [ObservableProperty] private string? _currentlyPlayingId;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public string RecordingButtonText => IsRecording ? (IsPaused ? "Resume" : "Pause") : "Record";

    public ObservableCollection<RecordingItemViewModel> Recordings { get; } = new();

    public RecordingsViewModel( IConversationRecordingService recordingService
                               , IConversationRecorderApiClient? recorderApiClient = null )
    {
        _recordingService  = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
        _recorderApiClient = recorderApiClient;

        _recordingService.RecordingTimerTicked += OnTimerTicked;
        _recordingService.RecordingStateChanged += OnRecordingStateChanged;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var recordings = await _recordingService.GetRecordingsAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Recordings.Clear();
            foreach (var recording in recordings)
            {
                var item = new RecordingItemViewModel(recording)
                {
                    IsPlaying = (recording.Id == CurrentlyPlayingId && IsPlaying),
                    IsPlaybackPaused = (recording.Id == CurrentlyPlayingId && IsPlaybackPaused)
                };
                Recordings.Add(item);
            }
        });
    }

    [RelayCommand]
    public async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            if (IsPaused)
            {
                StatusMessage = "Resuming recording...";
                var resumed = await _recordingService.ResumeRecordingAsync();
                if (resumed)
                {
                    StatusMessage = "Recording resumed.";
                }
            }
            else
            {
                StatusMessage = "Pausing recording...";
                var paused = await _recordingService.PauseRecordingAsync();
                if (paused)
                {
                    StatusMessage = "Recording paused.";
                }
            }
        }
        else
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (permissionStatus != PermissionStatus.Granted)
            {
                StatusMessage = "Microphone permission is required to record conversations.";
                return;
            }

            StatusMessage = "Recording started...";
            var started = await _recordingService.StartRecordingAsync();
            if (!started)
            {
                StatusMessage = "Failed to start recording.";
            }
        }
    }

    [RelayCommand]
    public async Task StopRecordingAsync()
    {
        if (!IsRecording)
        {
            return;
        }

        StatusMessage = "Stopping recording...";
        var result = await _recordingService.StopRecordingAsync();
        if (result != null)
        {
            StatusMessage = "Recording saved successfully.";
        }
        else
        {
            StatusMessage = "Failed to save recording.";
        }

        await RefreshAsync();
    }

    [RelayCommand]
    public async Task PlayRecordingAsync(RecordingItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        if (CurrentlyPlayingId == item.Id)
        {
            if (IsPlaying && !IsPlaybackPaused)
            {
                await _recordingService.PausePlaybackAsync();
                StatusMessage = "Playback paused.";
                return;
            }
            if (IsPlaying && IsPlaybackPaused)
            {
                await _recordingService.ResumePlaybackAsync();
                StatusMessage = "Playback resumed.";
                return;
            }
        }

        StatusMessage = "Playing recording...";
        var success = await _recordingService.PlayRecordingAsync(item.Id);
        if (!success)
        {
            StatusMessage = "Failed to play recording file.";
        }
    }

    [RelayCommand]
    public async Task StopPlaybackAsync(RecordingItemViewModel? item)
    {
        await _recordingService.StopPlaybackAsync();
        StatusMessage = "Playback stopped.";
    }

    [RelayCommand]
    public async Task DeleteRecordingAsync(RecordingItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        StatusMessage = "Deleting recording...";
        var success = await _recordingService.DeleteRecordingAsync(item.Id);
        if (success)
        {
            StatusMessage = "Recording deleted.";
            await RefreshAsync();
        }
        else
        {
            StatusMessage = "Failed to delete recording.";
        }
    }

    [RelayCommand]
    public async Task TranscribeAndDiarizeAsync(RecordingItemViewModel? item)
    {
        if (item == null || _recorderApiClient == null)
        {
            StatusMessage = "API client not available for transcription.";
            return;
        }

        if (!File.Exists(item.RecordingPath))
        {
            StatusMessage = "Audio file not found.";
            return;
        }

        if (!Guid.TryParse(item.Id, out var conversationGuid))
        {
            StatusMessage = "Invalid conversation ID.";
            return;
        }

        try
        {
            item.IsTranscribing = true;
            StatusMessage = "Transcribing audio...";

            using (var stream1 = File.OpenRead(item.RecordingPath))
            {
                await _recorderApiClient.TranscribeRecordingAsync(conversationGuid, stream1);
            }

            StatusMessage = "Diarizing speakers...";
            using (var stream2 = File.OpenRead(item.RecordingPath))
            {
                await _recorderApiClient.DiarizeRecordingAsync(conversationGuid, stream2);
            }

            StatusMessage = "Loading transcript details...";
            var details = await _recorderApiClient.GetConversationDetailsAsync(conversationGuid);
            if (details != null)
            {
                item.LoadTranscript(details.Transcript, details.Participants);
                item.IsTranscriptExpanded = true;
                StatusMessage = "Transcription & Diarization complete.";
            }
            else
            {
                StatusMessage = "Transcript generated but details could not be rehydrated.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Transcription error: {ex.Message}";
        }
        finally
        {
            item.IsTranscribing = false;
        }
    }

    [RelayCommand]
    public async Task ToggleTranscriptAsync(RecordingItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        item.IsTranscriptExpanded = !item.IsTranscriptExpanded;

        if (item.IsTranscriptExpanded && item.Transcript == null && _recorderApiClient != null && Guid.TryParse(item.Id, out var conversationGuid))
        {
            try
            {
                var details = await _recorderApiClient.GetConversationDetailsAsync(conversationGuid);
                if (details != null && details.Transcript != null)
                {
                    item.LoadTranscript(details.Transcript, details.Participants);
                }
            }
            catch (Exception)
            {
                // Silently swallow fetch exception on toggle
            }
        }
    }

    [RelayCommand]
    public async Task SaveSpeakerMapAsync(RecordingItemViewModel? item)
    {
        if (item == null || _recorderApiClient == null || !Guid.TryParse(item.Id, out var conversationGuid))
        {
            return;
        }

        try
        {
            StatusMessage = "Updating speaker mapping...";
            var speakerMap = new Dictionary<string, string>
            {
                { "Speaker 1", string.IsNullOrWhiteSpace(item.SpeakerName1) ? "Speaker 1" : item.SpeakerName1 },
                { "Speaker 2", string.IsNullOrWhiteSpace(item.SpeakerName2) ? "Speaker 2" : item.SpeakerName2 }
            };

            await _recorderApiClient.MapParticipantsAsync(conversationGuid, speakerMap);

            var details = await _recorderApiClient.GetConversationDetailsAsync(conversationGuid);
            if (details != null)
            {
                item.LoadTranscript(details.Transcript, details.Participants);
                StatusMessage = "Speaker names updated.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update speakers: {ex.Message}";
        }
    }

    private void OnTimerTicked(object? sender, TimeSpan elapsed)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ElapsedTimeDisplay = elapsed.ToString(@"mm\:ss");
        });
    }

    private void OnRecordingStateChanged(object? sender, EventArgs args)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsRecording = _recordingService.IsRecording;
            IsPaused = _recordingService.IsPaused;
            IsPlaying = _recordingService.IsPlaying;
            IsPlaybackPaused = _recordingService.IsPlaybackPaused;
            CurrentlyPlayingId = _recordingService.CurrentlyPlayingId;

            OnPropertyChanged(nameof(RecordingButtonText));

            foreach (var item in Recordings)
            {
                item.IsPlaying = (item.Id == CurrentlyPlayingId && IsPlaying);
                item.IsPlaybackPaused = (item.Id == CurrentlyPlayingId && IsPlaybackPaused);
            }
        });
    }
}
