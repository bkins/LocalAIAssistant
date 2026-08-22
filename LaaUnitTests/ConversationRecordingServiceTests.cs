using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAIAssistant.Services.Recordings;
using Moq;
using Plugin.Maui.Audio;
using Xunit;

namespace LaaUnitTests;

public class ConversationRecordingServiceTests
{
    private readonly Mock<IAudioManager> _audioManagerMock = new();
    private readonly Mock<IAudioRecorder> _audioRecorderMock = new();
    private readonly Mock<IConversationRecordingStore> _recordingStoreMock = new();
    private readonly ConversationRecordingService _service;

    public ConversationRecordingServiceTests()
    {
        _audioManagerMock.Setup(m => m.CreateRecorder()).Returns(_audioRecorderMock.Object);
        _service = new ConversationRecordingService(_audioManagerMock.Object, _recordingStoreMock.Object);
    }

    [Fact]
    public async Task StartRecordingAsync_StartsRecorder_AndSetsIsRecordingTrue()
    {
        _audioRecorderMock.Setup(r => r.StartAsync()).Returns(Task.CompletedTask);

        var started = await _service.StartRecordingAsync();

        Assert.True(started);
        Assert.True(_service.IsRecording);
        _audioRecorderMock.Verify(r => r.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StartRecordingAsync_ReturnsFalse_WhenAlreadyRecording()
    {
        _audioRecorderMock.Setup(r => r.StartAsync()).Returns(Task.CompletedTask);
        await _service.StartRecordingAsync();

        var secondStart = await _service.StartRecordingAsync();

        Assert.False(secondStart);
    }

    [Fact]
    public async Task StopRecordingAsync_ReturnsNull_WhenNotRecording()
    {
        var result = await _service.StopRecordingAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordingsAsync_ReturnsRecordingsFromStore()
    {
        var expectedList = new List<ConversationRecording>
        {
            new ConversationRecording { Id = "rec-1" }
        };
        _recordingStoreMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(expectedList);

        var result = await _service.GetRecordingsAsync();

        Assert.Single(result);
        Assert.Equal("rec-1", result[0].Id);
    }

    [Fact]
    public async Task DeleteRecordingAsync_CallsStoreSoftDelete_AndReturnsTrue()
    {
        var recording = new ConversationRecording { Id = "rec-to-del", RecordingPath = "" };
        _recordingStoreMock.Setup(s => s.GetByIdAsync("rec-to-del", It.IsAny<CancellationToken>()))
                           .ReturnsAsync(recording);
        _recordingStoreMock.Setup(s => s.SoftDeleteAsync("rec-to-del", It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

        var result = await _service.DeleteRecordingAsync("rec-to-del");

        Assert.True(result);
        _recordingStoreMock.Verify(s => s.SoftDeleteAsync("rec-to-del", It.IsAny<CancellationToken>()), Times.Once);
    }
}
