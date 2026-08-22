using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace LocalAIAssistant.Services.Recordings;

public class ConversationRecordingService : IConversationRecordingService
{
    private readonly IAudioManager _audioManager;
    private readonly IConversationRecordingStore _recordingStore;
    private readonly string _recordingsDirectory;

    private IAudioRecorder? _audioRecorder;
    private IAudioPlayer? _audioPlayer;
    private Timer? _timer;
    private DateTimeOffset _recordingStartTime;

    public bool IsRecording { get; private set; }

    public bool IsPlaying { get; private set; }

    public TimeSpan ElapsedRecordingTime { get; private set; }

    public string? CurrentlyPlayingId { get; private set; }

    public event EventHandler<TimeSpan>? RecordingTimerTicked;

    public event EventHandler? RecordingStateChanged;

    public ConversationRecordingService( IAudioManager               audioManager
                                        , IConversationRecordingStore recordingStore
                                        , string?                     recordingsDirectory = null )
    {
        _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
        _recordingStore = recordingStore ?? throw new ArgumentNullException(nameof(recordingStore));

        if (!string.IsNullOrWhiteSpace(recordingsDirectory))
        {
            _recordingsDirectory = recordingsDirectory;
        }
        else
        {
            try
            {
                _recordingsDirectory = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "Recordings");
            }
            catch (Exception)
            {
                _recordingsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");
            }
        }

        if (!Directory.Exists(_recordingsDirectory))
        {
            Directory.CreateDirectory(_recordingsDirectory);
        }
    }

    public async Task<bool> StartRecordingAsync( CancellationToken cancellationToken = default )
    {
        if (IsRecording)
        {
            return false;
        }

        try
        {
            _audioRecorder = _audioManager.CreateRecorder();
            await _audioRecorder.StartAsync();

            IsRecording = true;
            _recordingStartTime = DateTimeOffset.UtcNow;
            ElapsedRecordingTime = TimeSpan.Zero;

            _timer?.Dispose();
            _timer = new Timer(OnTimerTick, null, 1000, 1000);

            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            IsRecording = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public async Task<ConversationRecording?> StopRecordingAsync( CancellationToken cancellationToken = default )
    {
        if (!IsRecording || _audioRecorder == null)
        {
            return null;
        }

        try
        {
            _timer?.Dispose();
            _timer = null;

            var audioSource = await _audioRecorder.StopAsync();
            var endedAt = DateTimeOffset.UtcNow;
            var duration = endedAt - _recordingStartTime;

            IsRecording = false;
            ElapsedRecordingTime = duration;

            var conversationId = Guid.NewGuid().ToString();
            var filePath = Path.Combine(_recordingsDirectory, $"recording_{conversationId}.wav");

            if (audioSource != null)
            {
                using var stream = audioSource.GetAudioStream();
                if (stream != null)
                {
                    using var fileStream = File.Create(filePath);
                    await stream.CopyToAsync(fileStream, cancellationToken);
                }
            }

            var recording = new ConversationRecording
            {
                Id = conversationId,
                StartedAt = _recordingStartTime,
                EndedAt = endedAt,
                Duration = duration,
                RecordingPath = filePath,
                Status = "Recorded"
            };

            await _recordingStore.SaveAsync(recording, cancellationToken);
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);

            return recording;
        }
        catch (Exception)
        {
            IsRecording = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return null;
        }
    }

    public Task<IReadOnlyList<ConversationRecording>> GetRecordingsAsync( CancellationToken cancellationToken = default )
    {
        return _recordingStore.GetAllAsync(cancellationToken);
    }

    public async Task<bool> DeleteRecordingAsync( string            id
                                                 , CancellationToken cancellationToken = default )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var recording = await _recordingStore.GetByIdAsync(id, cancellationToken);
        if (recording == null)
        {
            return false;
        }

        if (CurrentlyPlayingId == id)
        {
            await StopPlaybackAsync();
        }

        if (!string.IsNullOrWhiteSpace(recording.RecordingPath) && File.Exists(recording.RecordingPath))
        {
            try
            {
                File.Delete(recording.RecordingPath);
            }
            catch (Exception)
            {
                // File deletion failure swallowed to preserve store consistency
            }
        }

        var success = await _recordingStore.SoftDeleteAsync(id, cancellationToken);
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        return success;
    }

    public async Task<bool> PlayRecordingAsync( string            id
                                               , CancellationToken cancellationToken = default )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var recording = await _recordingStore.GetByIdAsync(id, cancellationToken);
        if (recording == null || string.IsNullOrWhiteSpace(recording.RecordingPath) || !File.Exists(recording.RecordingPath))
        {
            return false;
        }

        await StopPlaybackAsync();

        try
        {
            var fileStream = File.OpenRead(recording.RecordingPath);
            _audioPlayer = _audioManager.CreatePlayer(fileStream);

            CurrentlyPlayingId = id;
            IsPlaying = true;

            _audioPlayer.PlaybackEnded += (_, _) =>
            {
                IsPlaying = false;
                CurrentlyPlayingId = null;
                RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            };

            _audioPlayer.Play();
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            IsPlaying = false;
            CurrentlyPlayingId = null;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public Task StopPlaybackAsync()
    {
        if (_audioPlayer != null && IsPlaying)
        {
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }

        IsPlaying = false;
        CurrentlyPlayingId = null;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    private void OnTimerTick(object? state)
    {
        if (IsRecording)
        {
            ElapsedRecordingTime = DateTimeOffset.UtcNow - _recordingStartTime;
            RecordingTimerTicked?.Invoke(this, ElapsedRecordingTime);
        }
    }
}
