using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalAIAssistant.Services.Recordings;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace LocalAIAssistant.ViewModels;

public partial class RecordingsViewModel : ObservableObject
{
    private readonly IConversationRecordingService _recordingService;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private string _elapsedTimeDisplay = "00:00";
    [ObservableProperty] private string? _currentlyPlayingId;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<RecordingItemViewModel> Recordings { get; } = new();

    public RecordingsViewModel(IConversationRecordingService recordingService)
    {
        _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));

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
                    IsPlaying = (recording.Id == CurrentlyPlayingId && IsPlaying)
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
    public async Task PlayRecordingAsync(RecordingItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        if (IsPlaying && CurrentlyPlayingId == item.Id)
        {
            await _recordingService.StopPlaybackAsync();
            StatusMessage = "Playback stopped.";
            return;
        }

        StatusMessage = "Playing recording...";
        var success = await _recordingService.PlayRecordingAsync(item.Id);
        if (!success)
        {
            StatusMessage = "Failed to play recording file.";
        }
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
            IsPlaying = _recordingService.IsPlaying;
            CurrentlyPlayingId = _recordingService.CurrentlyPlayingId;

            foreach (var item in Recordings)
            {
                item.IsPlaying = (item.Id == CurrentlyPlayingId && IsPlaying);
            }
        });
    }
}
