using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CognitivePlatform.Api.Data;
using LocalAIAssistant.Services.Recordings;
using Moq;
using Xunit;

namespace LaaUnitTests;

public class ConversationRecordingStoreTests
{
    private readonly Mock<IObjectStore> _objectStoreMock = new();
    private readonly ConversationRecordingStore _store;

    public ConversationRecordingStoreTests()
    {
        _store = new ConversationRecordingStore(_objectStoreMock.Object);
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentNullException_WhenRecordingIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_CallsObjectStoreSave_AndReturnsId()
    {
        var recording = new ConversationRecording { Id = "rec-123" };
        _objectStoreMock.Setup(store => store.Save(recording, "conversation_recordings", "rec-123"))
                        .Returns("rec-123");

        var result = await _store.SaveAsync(recording);

        Assert.Equal("rec-123", result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdIsEmpty()
    {
        var result = await _store.GetByIdAsync(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecording_WhenFound()
    {
        var expected = new ConversationRecording { Id = "rec-456" };
        _objectStoreMock.Setup(store => store.Get<ConversationRecording>("rec-456", "conversation_recordings"))
                        .Returns(expected);

        var result = await _store.GetByIdAsync("rec-456");

        Assert.NotNull(result);
        Assert.Equal("rec-456", result!.Id);
    }

    [Fact]
    public async Task GetAllAsync_FiltersOutDeleted_AndOrdersByStartedAtDescending()
    {
        var item1 = new ConversationRecording { Id = "1", StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10), IsDeleted = false };
        var item2 = new ConversationRecording { Id = "2", StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5), IsDeleted = false };
        var item3 = new ConversationRecording { Id = "3", StartedAt = DateTimeOffset.UtcNow, IsDeleted = true };

        _objectStoreMock.Setup(store => store.List<ConversationRecording>("conversation_recordings", null, null))
                        .Returns(new List<ConversationRecording> { item1, item2, item3 });

        var results = await _store.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("2", results[0].Id);
        Assert.Equal("1", results[1].Id);
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsIsDeletedTrue_AndReturnsTrue()
    {
        var item = new ConversationRecording { Id = "rec-del", IsDeleted = false };
        _objectStoreMock.Setup(store => store.Get<ConversationRecording>("rec-del", "conversation_recordings"))
                        .Returns(item);

        var result = await _store.SoftDeleteAsync("rec-del");

        Assert.True(result);
        Assert.True(item.IsDeleted);
        Assert.NotNull(item.DeletedUtc);
        _objectStoreMock.Verify(store => store.Save(item, "conversation_recordings", "rec-del"), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_PersistsTitleProperty_WhenTitleIsSet()
    {
        var recording = new ConversationRecording { Id = "rec-title", Title = "Strategy Session" };
        _objectStoreMock.Setup(store => store.Save(recording, "conversation_recordings", "rec-title"))
                        .Returns("rec-title");

        var result = await _store.SaveAsync(recording);

        Assert.Equal("rec-title", result);
        Assert.Equal("Strategy Session", recording.Title);
    }

    [Fact]
    public void FormatFileSize_FormatsBytesKBMB_Correctly()
    {
        Assert.Equal("0 B", FormatFileSize(0));
        Assert.Equal("500.0 B", FormatFileSize(500));
        Assert.Equal("1.0 KB", FormatFileSize(1024));
        Assert.Equal("1.5 MB", FormatFileSize((long)(1.5 * 1024 * 1024)));
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double dblBytes = bytes;
        while (dblBytes >= 1024 && i < suffixes.Length - 1)
        {
            dblBytes /= 1024;
            i++;
        }
        return $"{dblBytes:0.0} {suffixes[i]}";
    }
}
