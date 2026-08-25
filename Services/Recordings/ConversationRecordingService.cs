using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private DateTimeOffset _segmentStartTime;
    private TimeSpan _accumulatedTime = TimeSpan.Zero;
    private readonly List<string> _segmentFilePaths = new();
    private string? _currentSegmentPath;

    public bool IsRecording { get; private set; }

    public bool IsPaused { get; private set; }

    public bool IsPlaying { get; private set; }

    public bool IsPlaybackPaused { get; private set; }

    public TimeSpan ElapsedRecordingTime { get; private set; }

    public string? CurrentlyPlayingId { get; private set; }

    public event EventHandler<TimeSpan>? RecordingTimerTicked;

    public event EventHandler? RecordingStateChanged;

    public ConversationRecordingService( IAudioManager               audioManager
                                        , IConversationRecordingStore recordingStore
                                        , string?                     environmentName = null
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
            _recordingsDirectory = Path.Combine(GetPersistentDataDirectory(environmentName), "Recordings");
        }

        if (!Directory.Exists(_recordingsDirectory))
        {
            Directory.CreateDirectory(_recordingsDirectory);
        }
    }

    public static string GetPersistentDataDirectory(string? environmentName = null)
    {
        var env = string.IsNullOrWhiteSpace(environmentName) ? "Dev" : environmentName;
        try
        {
            string baseDir;
            if (OperatingSystem.IsWindows())
            {
                if (Directory.Exists(@"C:\CP\Data"))
                {
                    baseDir = Path.Combine(@"C:\CP\Data", env, "LocalAIAssistant");
                }
                else
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    baseDir = Path.Combine(localAppData, "CognitivePlatform", env);
                }
            }
            else
            {
                baseDir = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, env);
            }

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            return baseDir;
        }
        catch (Exception)
        {
            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, env);
            if (!Directory.Exists(fallback))
            {
                Directory.CreateDirectory(fallback);
            }
            return fallback;
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
            _segmentFilePaths.Clear();
            _accumulatedTime = TimeSpan.Zero;
            _segmentStartTime = DateTimeOffset.UtcNow;
            _currentSegmentPath = Path.Combine(_recordingsDirectory, $"seg_{Guid.NewGuid()}.wav");

            _audioRecorder = _audioManager.CreateRecorder();
            await _audioRecorder.StartAsync();

            IsRecording = true;
            IsPaused = false;
            ElapsedRecordingTime = TimeSpan.Zero;

            _timer?.Dispose();
            _timer = new Timer(OnTimerTick, null, 500, 500);

            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            IsRecording = false;
            IsPaused = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public async Task<bool> PauseRecordingAsync( CancellationToken cancellationToken = default )
    {
        if (!IsRecording || IsPaused || _audioRecorder == null)
        {
            return false;
        }

        try
        {
            var segmentDuration = DateTimeOffset.UtcNow - _segmentStartTime;
            _accumulatedTime += segmentDuration;

            var audioSource = await _audioRecorder.StopAsync();
            _audioRecorder = null;

            if (audioSource != null && _currentSegmentPath != null)
            {
                using var stream = audioSource.GetAudioStream();
                if (stream != null)
                {
                    using var fileStream = File.Create(_currentSegmentPath);
                    await stream.CopyToAsync(fileStream, cancellationToken);
                    _segmentFilePaths.Add(_currentSegmentPath);
                }
            }

            IsPaused = true;
            ElapsedRecordingTime = _accumulatedTime;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            IsPaused = true;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public async Task<bool> ResumeRecordingAsync( CancellationToken cancellationToken = default )
    {
        if (!IsRecording || !IsPaused)
        {
            return false;
        }

        try
        {
            _segmentStartTime = DateTimeOffset.UtcNow;
            _currentSegmentPath = Path.Combine(_recordingsDirectory, $"seg_{Guid.NewGuid()}.wav");

            _audioRecorder = _audioManager.CreateRecorder();
            await _audioRecorder.StartAsync();

            IsPaused = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<ConversationRecording?> StopRecordingAsync( CancellationToken cancellationToken = default )
    {
        if (!IsRecording)
        {
            return null;
        }

        try
        {
            _timer?.Dispose();
            _timer = null;

            if (!IsPaused && _audioRecorder != null)
            {
                var segmentDuration = DateTimeOffset.UtcNow - _segmentStartTime;
                _accumulatedTime += segmentDuration;

                var audioSource = await _audioRecorder.StopAsync();
                _audioRecorder = null;

                if (audioSource != null && _currentSegmentPath != null)
                {
                    using var stream = audioSource.GetAudioStream();
                    if (stream != null)
                    {
                        using var fileStream = File.Create(_currentSegmentPath);
                        await stream.CopyToAsync(fileStream, cancellationToken);
                        _segmentFilePaths.Add(_currentSegmentPath);
                    }
                }
            }

            var endedAt = DateTimeOffset.UtcNow;
            var totalDuration = _accumulatedTime;

            IsRecording = false;
            IsPaused = false;
            ElapsedRecordingTime = totalDuration;

            var conversationId = Guid.NewGuid().ToString();
            var finalPath = Path.Combine(_recordingsDirectory, $"recording_{conversationId}.wav");

            await MergeWavFilesAsync(_segmentFilePaths, finalPath, cancellationToken);
            CleanUpSegmentFiles(_segmentFilePaths);

            var recording = new ConversationRecording
            {
                Id = conversationId,
                StartedAt = endedAt - totalDuration,
                EndedAt = endedAt,
                Duration = totalDuration,
                RecordingPath = finalPath,
                Status = "Recorded"
            };

            await _recordingStore.SaveAsync(recording, cancellationToken);
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);

            return recording;
        }
        catch (Exception)
        {
            IsRecording = false;
            IsPaused = false;
            CleanUpSegmentFiles(_segmentFilePaths);
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return null;
        }
    }

    public async Task<IReadOnlyList<ConversationRecording>> GetRecordingsAsync( CancellationToken cancellationToken = default )
    {
        var list = await _recordingStore.GetAllAsync(cancellationToken);
        foreach (var item in list)
        {
            item.RecordingPath = ResolveRecordingPath(item.RecordingPath);
        }
        return list;
    }

    private string ResolveRecordingPath(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return string.Empty;
        }

        if (File.Exists(storedPath))
        {
            return storedPath;
        }

        var fileName = Path.GetFileName(storedPath);
        var candidatePath = Path.Combine(_recordingsDirectory, fileName);
        if (File.Exists(candidatePath))
        {
            return candidatePath;
        }

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var legacyPath = Path.Combine(localAppData, "CognitivePlatform", "Recordings", fileName);
            if (File.Exists(legacyPath))
            {
                File.Copy(legacyPath, candidatePath, overwrite: true);
                return candidatePath;
            }
        }
        catch
        {
            // Ignore migration fallback exception
        }

        return storedPath;
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
            IsPlaybackPaused = false;

            _audioPlayer.PlaybackEnded += (_, _) =>
            {
                IsPlaying = false;
                IsPlaybackPaused = false;
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
            IsPlaybackPaused = false;
            CurrentlyPlayingId = null;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public Task PausePlaybackAsync()
    {
        if (_audioPlayer != null && IsPlaying && !IsPlaybackPaused)
        {
            _audioPlayer.Pause();
            IsPlaybackPaused = true;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    public Task ResumePlaybackAsync()
    {
        if (_audioPlayer != null && IsPlaying && IsPlaybackPaused)
        {
            _audioPlayer.Play();
            IsPlaybackPaused = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    public Task StopPlaybackAsync()
    {
        if (_audioPlayer != null && (IsPlaying || IsPlaybackPaused))
        {
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }

        IsPlaying = false;
        IsPlaybackPaused = false;
        CurrentlyPlayingId = null;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);

        return Task.CompletedTask;
    }

    private void OnTimerTick(object? state)
    {
        if (IsRecording && !IsPaused)
        {
            ElapsedRecordingTime = _accumulatedTime + (DateTimeOffset.UtcNow - _segmentStartTime);
            RecordingTimerTicked?.Invoke(this, ElapsedRecordingTime);
        }
        else if (IsRecording && IsPaused)
        {
            ElapsedRecordingTime = _accumulatedTime;
            RecordingTimerTicked?.Invoke(this, ElapsedRecordingTime);
        }
    }

    private static async Task MergeWavFilesAsync(List<string> inputFiles, string outputFile, CancellationToken cancellationToken)
    {
        var validFiles = inputFiles.Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f)).ToList();
        if (validFiles.Count == 0)
        {
            return;
        }

        if (validFiles.Count == 1)
        {
            File.Copy(validFiles[0], outputFile, overwrite: true);
            return;
        }

        using var outputStream = File.Create(outputFile);
        byte[]? header = null;
        int totalPcmBytes = 0;

        foreach (var filePath in validFiles)
        {
            using var inputStream = File.OpenRead(filePath);
            if (inputStream.Length < 44)
            {
                continue;
            }

            if (header == null)
            {
                header = new byte[44];
                await inputStream.ReadExactlyAsync(header, 0, 44, cancellationToken);
                await outputStream.WriteAsync(header, 0, 44, cancellationToken);
            }
            else
            {
                inputStream.Seek(44, SeekOrigin.Begin);
            }

            var pcmBuffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(pcmBuffer, 0, pcmBuffer.Length, cancellationToken)) > 0)
            {
                await outputStream.WriteAsync(pcmBuffer, 0, bytesRead, cancellationToken);
                totalPcmBytes += bytesRead;
            }
        }

        if (header != null && outputStream.Length >= 44)
        {
            outputStream.Seek(4, SeekOrigin.Begin);
            var riffSizeBits = BitConverter.GetBytes((int)(outputStream.Length - 8));
            await outputStream.WriteAsync(riffSizeBits, 0, 4, cancellationToken);

            outputStream.Seek(40, SeekOrigin.Begin);
            var dataSizeBits = BitConverter.GetBytes(totalPcmBytes);
            await outputStream.WriteAsync(dataSizeBits, 0, 4, cancellationToken);
        }
    }

    private static void CleanUpSegmentFiles(List<string> segmentFiles)
    {
        foreach (var file in segmentFiles)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception)
            {
                // Swallowed
            }
        }
    }
}
