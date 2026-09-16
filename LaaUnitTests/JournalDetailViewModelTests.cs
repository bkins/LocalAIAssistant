using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.Journal;
using LocalAIAssistant.Core.Media;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Knowledge.Journals.Models;
using LocalAIAssistant.Knowledge.Journals.ViewModels;
using Moq;

namespace LaaUnitTests;

public class JournalDetailViewModelTests
{
    private readonly Mock<IJournalApiClient> _client = new();
    private readonly Mock<IMediaAttachmentApiClient> _media = new();
    private readonly JournalDetailViewModel _viewModel;

    public JournalDetailViewModelTests()
    {
        var factory = new Mock<IJournalApiClientFactory>();
        factory.Setup(service => service.Create()).Returns(_client.Object);
        _viewModel = new JournalDetailViewModel(factory.Object, _media.Object) { JournalId = Guid.NewGuid() };
        _media.Setup(service => service.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<MediaAttachmentDto>());
    }

    [Fact]
    public async Task LoadAsync_InvalidPayload_ShowsErrorAndStopsLoading()
    {
        _client.Setup(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new JsonException("Invalid journal state"));

        await _viewModel.LoadAsync();

        Assert.True(_viewModel.HasError);
        Assert.False(_viewModel.IsLoading);
        Assert.NotEmpty(_viewModel.ErrorMessage);
        Assert.IsType<JsonException>(_viewModel.CaughtException);
        _media.Verify(service => service.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_NotFound_ShowsErrorWithoutLoadingMedia()
    {
        _client.Setup(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((JournalEntryDto?)null);

        await _viewModel.LoadAsync();

        Assert.True(_viewModel.HasError);
        Assert.NotEmpty(_viewModel.ErrorMessage);
        _media.Verify(service => service.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_DtoError_ShowsFailureWithoutLoadingMedia()
    {
        _client.Setup(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new JournalEntryDto { Error = new DtoError { Message = "Unavailable" } });

        await _viewModel.LoadAsync();

        Assert.True(_viewModel.HasError);
        Assert.NotEmpty(_viewModel.ErrorMessage);
        _media.Verify(service => service.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_MediaFailure_KeepsJournalBodyAndReportsError()
    {
        _client.Setup(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new JournalEntryDto { Text = "Journal body" });
        _media.Setup(service => service.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("Media unavailable"));

        await _viewModel.LoadAsync();

        Assert.Equal("Journal body", _viewModel.Text);
        Assert.True(_viewModel.HasError);
        Assert.Contains("attachments", _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_FailedReload_DoesNotLeavePreviousEntryVisible()
    {
        _client.SetupSequence(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new JournalEntryDto { Text = "Previous body", Mood = "Happy", Tags = new[] { "old" } })
               .ReturnsAsync((JournalEntryDto?)null);

        await _viewModel.LoadAsync();
        await _viewModel.LoadAsync();

        Assert.Empty(_viewModel.Text);
        Assert.Empty(_viewModel.Tags);
        Assert.Null(_viewModel.Mood);
        Assert.True(_viewModel.HasError);
    }

    [Fact]
    public async Task LoadAsync_RetryAfterFailure_ClearsErrorAndDisplaysContent()
    {
        _client.SetupSequence(service => service.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new JournalEntryDto { Error = new DtoError { Message = "Unavailable" } })
               .ReturnsAsync(new JournalEntryDto { Text = "# Title\n\nBody", Tags = new[] { "source:ttr" }, State = JournalEntryState.Committed });

        await _viewModel.LoadAsync();
        await _viewModel.LoadAsync();

        Assert.False(_viewModel.HasError);
        Assert.Empty(_viewModel.ErrorMessage);
        Assert.Null(_viewModel.CaughtException);
        Assert.Equal("# Title\n\nBody", _viewModel.Text);
        Assert.Equal(new[] { "source:ttr" }, _viewModel.Tags);
        Assert.False(_viewModel.IsLoading);
    }
}
