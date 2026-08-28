using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAIAssistant.Core.ConversationRecorder;

namespace LocalAIAssistant.Services.Recordings;

public interface IConversationRecordingService
{
    bool IsRecording { get; }

    bool IsPaused { get; }

    bool IsPlaying { get; }

    bool IsPlaybackPaused { get; }

    TimeSpan ElapsedRecordingTime { get; }

    string? CurrentlyPlayingId { get; }

    bool IsCopilotEnabled { get; set; }

    IReadOnlyList<CopilotInsightDto> ActiveSessionInsights { get; }

    event EventHandler<TimeSpan>? RecordingTimerTicked;

    event EventHandler? RecordingStateChanged;

    event EventHandler<CopilotInsightDto>? CopilotInsightReceived;

    Task<bool> StartRecordingAsync( CancellationToken cancellationToken = default );

    Task<bool> PauseRecordingAsync( CancellationToken cancellationToken = default );

    Task<bool> ResumeRecordingAsync( CancellationToken cancellationToken = default );

    Task<ConversationRecording?> StopRecordingAsync( CancellationToken cancellationToken = default );

    Task<IReadOnlyList<ConversationRecording>> GetRecordingsAsync( CancellationToken cancellationToken = default );

    Task<bool> DeleteRecordingAsync( string            id
                                   , CancellationToken cancellationToken = default );

    Task<bool> PlayRecordingAsync( string            id
                                 , CancellationToken cancellationToken = default );

    Task PausePlaybackAsync();

    Task ResumePlaybackAsync();

    Task StopPlaybackAsync();
}
