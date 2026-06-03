using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.Core.Media;

namespace LaaUnitTests;

public class MediaAttachmentApiClientTests
{
    // ── UploadAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ReturnsDto_WhenApiResponds()
    {
        var id   = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new
        {
            id          = id
          , fileName    = "photo.jpg"
          , contentType = "image/jpeg"
          , fileSize    = 1024L
          , createdAt   = DateTimeOffset.UtcNow
          , ownerType   = "JournalEntry"
          , ownerId     = Guid.NewGuid()
        });

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.UploadAsync(Guid.NewGuid()
                                         , "photo.jpg"
                                         , "image/jpeg"
                                         , new MemoryStream(new byte[] { 1, 2, 3 }));

        Assert.NotNull(result);
        Assert.Equal(id,         result!.Id);
        Assert.Equal("photo.jpg", result.FileName);
        Assert.True(result.IsImage);
    }

    [Fact]
    public async Task UploadAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.UploadAsync(Guid.NewGuid()
                                         , "photo.jpg"
                                         , "image/jpeg"
                                         , new MemoryStream());

        Assert.Null(result);
    }

    [Fact]
    public async Task UploadAsync_Throws_WhenCancelled()
    {
        var sut       = BuildClient(HttpStatusCode.OK, "{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.UploadAsync(Guid.NewGuid(), "f.jpg", "image/jpeg", new MemoryStream(), cts.Token));
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsList_WhenApiResponds()
    {
        var journalId = Guid.NewGuid();
        var json      = JsonSerializer.Serialize(new[]
        {
            new { id = Guid.NewGuid(), fileName = "a.jpg", contentType = "image/jpeg", fileSize = 100L, createdAt = DateTimeOffset.UtcNow, ownerType = "JournalEntry", ownerId = journalId }
          , new { id = Guid.NewGuid(), fileName = "b.pdf", contentType = "application/pdf", fileSize = 200L, createdAt = DateTimeOffset.UtcNow, ownerType = "JournalEntry", ownerId = journalId }
        });

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.ListAsync(journalId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("a.jpg", result[0].FileName);
        Assert.True(result[0].IsImage);
        Assert.False(result[1].IsImage);
    }

    [Fact]
    public async Task ListAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.ListAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.ListAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenApiReturnsSuccess()
    {
        var sut    = BuildClient(HttpStatusCode.NoContent, string.Empty);
        var result = await sut.DeleteAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenApiReturnsError()
    {
        var sut    = BuildClient(HttpStatusCode.InternalServerError, string.Empty);
        var result = await sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    // ── DownloadAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_ReturnsStream_WhenApiResponds()
    {
        var sut    = BuildClient(HttpStatusCode.OK, "file-bytes");
        var result = await sut.DownloadAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.DownloadAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.DownloadAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── IsImage computed property ─────────────────────────────────────────────

    [Theory]
    [InlineData("image/jpeg",      true)]
    [InlineData("image/png",       true)]
    [InlineData("IMAGE/WEBP",      true)]
    [InlineData("application/pdf", false)]
    [InlineData("video/mp4",       false)]
    [InlineData("",                false)]
    public void IsImage_ReflectsContentType(string contentType, bool expected)
    {
        var dto = new MediaAttachmentDto { ContentType = contentType };

        Assert.Equal(expected, dto.IsImage);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MediaAttachmentApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new MediaAttachmentApiClient(client);
    }

    private static MediaAttachmentApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
                     {
                         BaseAddress = new Uri("http://localhost/")
                     };
        return new MediaAttachmentApiClient(client);
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
        {
            ct.ThrowIfCancellationRequested();

            return Task.FromResult(new HttpResponseMessage(_status)
                                   {
                                       Content = new StringContent(_content
                                                                  , Encoding.UTF8
                                                                  , "application/json")
                                   });
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request
                                                              , CancellationToken  ct )
            => throw new HttpRequestException("Simulated network failure");
    }
}
