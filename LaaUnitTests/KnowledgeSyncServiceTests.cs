using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.CognitivePlatform.CpClients.Knowledge;
using LocalAIAssistant.Knowledge.Inbox;
using Moq;

namespace LaaUnitTests;

public class KnowledgeSyncServiceTests
{
    [Fact]
    public async Task ReadLocalAsync_DoesNotReadOnCallingContext()
    {
        var context = new SynchronizationContext();
        SynchronizationContext? readContext = context;
        var store = new Mock<ILocalKnowledgeStore>();
        store.Setup(service => service.List()).Callback(() => readContext = SynchronizationContext.Current)
             .Returns(Array.Empty<KnowledgeItem>());
        var sut = new KnowledgeSyncService(Mock.Of<IKnowledgeClientFactory>(), store.Object, Mock.Of<IConnectivityReporter>());
        var previous = SynchronizationContext.Current;
        Task<IReadOnlyList<KnowledgeItem>> operation;

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            operation = sut.ReadLocalAsync();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        var result = await operation;

        Assert.NotSame(context, readContext);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SyncAsync_FailedFetch_DoesNotClearCacheAndReleasesGate()
    {
        var factory = new Mock<IKnowledgeClientFactory>();
        var client = new Mock<IKnowledgeApiClient>();
        var store = new Mock<ILocalKnowledgeStore>();
        store.Setup(service => service.List()).Returns(Array.Empty<KnowledgeItem>());
        client.Setup(service => service.GetKnowledgeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        factory.Setup(service => service.Create()).Returns(client.Object);
        var connectivity = new Mock<IConnectivityReporter>();
        connectivity.Setup(service => service.Online()).Returns(true);
        var sut = new KnowledgeSyncService(factory.Object, store.Object, connectivity.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.SyncAsync());
        await sut.ReadLocalAsync().WaitAsync(TimeSpan.FromSeconds(5));

        store.Verify(service => service.Clear(), Times.Never);
        store.Verify(service => service.Save(It.IsAny<KnowledgeItem>()), Times.Never);
    }

    [Fact]
    public async Task ReadLocalAsync_ConcurrentSync_DoesNotObservePartialCache()
    {
        var factory = new Mock<IKnowledgeClientFactory>();
        var client = new Mock<IKnowledgeApiClient>();
        var store = new Mock<ILocalKnowledgeStore>();
        var connectivity = new Mock<IConnectivityReporter>();
        connectivity.Setup(service => service.Online()).Returns(true);
        client.Setup(service => service.GetKnowledgeAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[] { new KnowledgeItem { Id = Guid.NewGuid() } });
        factory.Setup(service => service.Create()).Returns(client.Object);
        var clearing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var completed = false;
        var readPartial = false;
        store.Setup(service => service.Clear()).Callback(() =>
        {
            clearing.SetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
        });
        store.Setup(service => service.Save(It.IsAny<KnowledgeItem>())).Callback(() => completed = true);
        store.Setup(service => service.List()).Callback(() => readPartial = !completed).Returns(Array.Empty<KnowledgeItem>());
        var sut = new KnowledgeSyncService(factory.Object, store.Object, connectivity.Object);

        var sync = sut.SyncAsync();
        await clearing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<IReadOnlyList<KnowledgeItem>> read;
        try
        {
            read = sut.ReadLocalAsync();
            Assert.False(read.IsCompleted);
        }
        finally
        {
            release.Set();
        }
        await Task.WhenAll(sync, read).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(readPartial);
    }

    [Fact]
    public async Task SyncAsync_CompletedApiRequest_DoesNotWriteOnCallingContext()
    {
        var client = new Mock<IKnowledgeApiClient>();
        var factory = new Mock<IKnowledgeClientFactory>();
        var store = new Mock<ILocalKnowledgeStore>();
        var connectivity = new Mock<IConnectivityReporter>();
        var context = new SynchronizationContext();
        var cacheUsedCallerContext = false;
        client.Setup(service => service.GetKnowledgeAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[] { new KnowledgeItem { Id = Guid.NewGuid() } });
        factory.Setup(service => service.Create()).Returns(client.Object);
        connectivity.Setup(service => service.Online()).Returns(true);
        store.Setup(service => service.Clear()).Callback(() => cacheUsedCallerContext |= SynchronizationContext.Current == context);
        store.Setup(service => service.Save(It.IsAny<KnowledgeItem>())).Callback(() => cacheUsedCallerContext |= SynchronizationContext.Current == context);
        var sut = new KnowledgeSyncService(factory.Object, store.Object, connectivity.Object);
        var previous = SynchronizationContext.Current;
        Task operation;

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            operation = sut.SyncAsync();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        await operation;

        Assert.False(cacheUsedCallerContext);
        store.Verify(service => service.Clear(), Times.Once);
        store.Verify(service => service.Save(It.IsAny<KnowledgeItem>()), Times.Once);
    }
}
