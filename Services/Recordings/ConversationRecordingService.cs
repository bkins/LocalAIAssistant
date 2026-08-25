using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LocalAIAssistant.Core.ConversationRecorder;
using Plugin.Maui.Audio;

namespace LocalAIAssistant.Services.Recordings;

public class ConversationRecordingService : IConversationRecordingService
{
    private readonly IAudioManager _audioManager;
    private readonly IConversationRecordingStore _recordingStore;
    private readonly IConversationRecorderApiClient? _apiClient;
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

    public ConversationRecordingService( IAudioManager                  audioManager
                                        , IConversationRecordingStore    recordingStore
                                        , IConversationRecorderApiClient? apiClient = null
                                        , string?                     environmentName = null
                                        , string?                     recordingsDirectory = null )
    {
        _audioManager   = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
        _recordingStore = recordingStore ?? throw new ArgumentNullException(nameof(recordingStore));
        _apiClient      = apiClient;

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
            await StopPlaybackAsync();

            _accumulatedTime = TimeSpan.Zero;
            _segmentFilePaths.Clear();
            _segmentStartTime = DateTimeOffset.UtcNow;

            _currentSegmentPath = Path.Combine(_recordingsDirectory, $"seg_{Guid.NewGuid()}.wav");
            _audioRecorder = _audioManager.CreateRecorder();

            await _audioRecorder.StartAsync(_currentSegmentPath);

            IsRecording = true;
            IsPaused = false;
            ElapsedRecordingTime = TimeSpan.Zero;

            _timer = new Timer(OnTimerTick, null, 1000, 1000);
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }
        catch (Exception)
        {
            IsRecording = false;
            IsPaused = false;
            CleanUpSegmentFiles(_segmentFilePaths);
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
            _timer?.Dispose();
            _timer = null;

            var segmentDuration = DateTimeOffset.UtcNow - _segmentStartTime;
            _accumulatedTime += segmentDuration;

            var audioSource = await _audioRecorder.StopAsync();
            _audioRecorder = null;

            if (_currentSegmentPath != null)
            {
                if (audioSource != null)
                {
                    try
                    {
                        using var stream = audioSource.GetAudioStream();
                        if (stream != null)
                        {
                            using var fileStream = File.Create(_currentSegmentPath);
                            await stream.CopyToAsync(fileStream, cancellationToken);
                        }
                    }
                    catch
                    {
                        // Native recorder may write directly to file on disk
                    }
                }

                if (File.Exists(_currentSegmentPath) && new FileInfo(_currentSegmentPath).Length > 0)
                {
                    if (!_segmentFilePaths.Contains(_currentSegmentPath))
                    {
                        _segmentFilePaths.Add(_currentSegmentPath);
                    }
                }
            }

            IsPaused = true;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
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

            await _audioRecorder.StartAsync(_currentSegmentPath);

            IsPaused = false;
            _timer = new Timer(OnTimerTick, null, 1000, 1000);
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

                if (_currentSegmentPath != null)
                {
                    if (audioSource != null)
                    {
                        try
                        {
                            using var stream = audioSource.GetAudioStream();
                            if (stream != null)
                            {
                                using var fileStream = File.Create(_currentSegmentPath);
                                await stream.CopyToAsync(fileStream, cancellationToken);
                            }
                        }
                        catch
                        {
                            // Native recorder may write directly to file on disk
                        }
                    }

                    if (File.Exists(_currentSegmentPath) && new FileInfo(_currentSegmentPath).Length > 0)
                    {
                        if (!_segmentFilePaths.Contains(_currentSegmentPath))
                        {
                            _segmentFilePaths.Add(_currentSegmentPath);
                        }
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

            if (_apiClient != null && File.Exists(finalPath) && Guid.TryParse(conversationId, out var parsedGuid))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var uploadStream = File.OpenRead(finalPath);
                        await _apiClient.UploadAudioAsync(parsedGuid, uploadStream);
                    }
                    catch
                    {
                        // Background upload error swallowed
                    }
                }, cancellationToken);
            }

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
        var localList = (await _recordingStore.GetAllAsync(cancellationToken)).ToList();

        if (_apiClient != null)
        {
            try
            {
                var remoteRecords = await _apiClient.SearchConversationsAsync(cancellationToken: cancellationToken);
                if (remoteRecords != null && remoteRecords.Count > 0)
                {
                    var localDict = localList.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

                    foreach (var remote in remoteRecords)
                    {
                        var idStr = remote.Id.ToString();
                        if (!localDict.TryGetValue(idStr, out var localItem))
                        {
                            localItem = new ConversationRecording
                            {
                                Id = idStr,
                                Title = remote.Title,
                                StartedAt = remote.RecordedAtUtc,
                                EndedAt = remote.RecordedAtUtc + remote.Duration,
                                Duration = remote.Duration,
                                RecordingPath = string.IsNullOrWhiteSpace(remote.AudioFilePath) ? $"recording_{idStr}.wav" : remote.AudioFilePath,
                                Status = remote.Status
                            };
                            await _recordingStore.SaveAsync(localItem, cancellationToken);
                        }
                        else if (!string.IsNullOrWhiteSpace(remote.Title) && localItem.Title != remote.Title)
                        {
                            localItem.Title = remote.Title;
                            await _recordingStore.SaveAsync(localItem, cancellationToken);
                        }
                    }

                    localList = (await _recordingStore.GetAllAsync(cancellationToken)).ToList();
                }
            }
            catch
            {
                // Network fetch exception swallowed
            }
        }

        foreach (var item in localList)
        {
            item.RecordingPath = ResolveRecordingPath(item.RecordingPath);
        }
        return localList;
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
        if (recording == null)
        {
            return false;
        }

        await StopPlaybackAsync();

        var resolvedPath = ResolveRecordingPath(recording.RecordingPath);

        Stream? audioStream = null;
        if (File.Exists(resolvedPath))
        {
            audioStream = File.OpenRead(resolvedPath);
        }
        else if (_apiClient != null && Guid.TryParse(id, out var parsedGuid))
        {
            audioStream = await _apiClient.GetAudioStreamAsync(parsedGuid, cancellationToken);
        }

        if (audioStream == null)
        {
            return false;
        }

        try
        {
            _audioPlayer = _audioManager.CreatePlayer(audioStream);
            _audioPlayer.PlaybackEnded += OnPlaybackEnded;
            _audioPlayer.Play();

            CurrentlyPlayingId = id;
            IsPlaying = true;
            IsPlaybackPaused = false;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }
        catch (Exception)
        {
            audioStream.Dispose();
            IsPlaying = false;
            IsPlaybackPaused = false;
            CurrentlyPlayingId = null;
            RecordingStateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }
    }

    public Task PausePlaybackAsync()
    {
        if (!IsPlaying || IsPlaybackPaused || _audioPlayer == null)
        {
            return Task.CompletedTask;
        }

        _audioPlayer.Pause();
        IsPlaybackPaused = true;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ResumePlaybackAsync()
    {
        if (!IsPlaying || !IsPlaybackPaused || _audioPlayer == null)
        {
            return Task.CompletedTask;
        }

        _audioPlayer.Play();
        IsPlaybackPaused = false;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopPlaybackAsync()
    {
        if (_audioPlayer != null)
        {
            try
            {
                _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
                if (_audioPlayer.IsPlaying)
                {
                    _audioPlayer.Stop();
                }
                _audioPlayer.Dispose();
            }
            catch (Exception)
            {
                // Swallowed player disposal exceptions
            }
            finally
            {
                _audioPlayer = null;
            }
        }

        IsPlaying = false;
        IsPlaybackPaused = false;
        CurrentlyPlayingId = null;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void OnTimerTick(object? state)
    {
        var elapsed = _accumulatedTime + (DateTimeOffset.UtcNow - _segmentStartTime);
        ElapsedRecordingTime = elapsed;
        RecordingTimerTicked?.Invoke(this, elapsed);
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        IsPlaying = false;
        IsPlaybackPaused = false;
        CurrentlyPlayingId = null;
        RecordingStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static async Task MergeWavFilesAsync(List<string> segmentPaths, string outputPath, CancellationToken cancellationToken)
    {
        var validSegments = segmentPaths.Where(File.Exists).ToList();
        if (validSegments.Count == 0)
        {
            return;
        }

        if (validSegments.Count == 1)
        {
            File.Copy(validSegments[0], outputPath, overwrite: true);
            return;
        }

        byte[] headerBytes = await File.ReadAllBytesAsync(validSegments[0], cancellationToken);
        if (headerBytes.Length < 44)
        {
            File.Copy(validSegments[0], outputPath, overwrite: true);
            return;
        }

        using var outputStream = File.Create(outputPath);
        await outputStream.WriteAsync(headerBytes, 0, 44, cancellationToken);

        uint totalPcmBytes = 0;
        foreach (var segmentFile in validSegments)
        {
            var bytes = await File.ReadAllBytesAsync(segmentFile, cancellationToken);
            if (bytes.Length > 44)
            {
                var pcmChunkLength = bytes.Length - 44;
                await outputStream.WriteAsync(bytes, 44, pcmChunkLength, cancellationToken);
                totalPcmBytes += (uint)pcmChunkLength;
            }
        }

        outputStream.Seek(4, SeekOrigin.Begin);
        var riffChunkSize = BitConverter.GetBytes(totalPcmBytes + 36);
        await outputStream.WriteAsync(riffChunkSize, 0, 4, cancellationToken);

        outputStream.Seek(40, SeekOrigin.Begin);
        var dataSubchunkSize = BitConverter.GetBytes(totalPcmBytes);
        await outputStream.WriteAsync(dataSubchunkSize, 0, 4, cancellationToken);
    }

    private static void CleanUpSegmentFiles(List<string> segmentPaths)
    {
        foreach (var path in segmentPaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // Segment cleanup exception swallowed
            }
        }
        segmentPaths.Clear();
    }
}
