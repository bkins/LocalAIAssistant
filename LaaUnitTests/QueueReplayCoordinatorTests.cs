using CP.Client.Core.Common.ConnectivityToApi;
using LocalAIAssistant.Services;
using LocalAIAssistant.Services.Interfaces;
using LocalAIAssistant.Services.Logging.Interfaces;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaaUnitTests;

public class QueueReplayCoordinatorTests
{
    private readonly Mock<IOfflineQueueService> _queueMock        = new();
    private readonly Mock<IConnectivityState>   _connectivityMock = new();
    private readonly Mock<ILoggingService>      _loggerMock       = new();

    [Fact]
    public void Constructor_WhenOnline_TriggersProcessQueue()
    {
        _connectivityMock.Setup(conn => conn.IsOffline).Returns(false);

        var coordinator = new QueueReplayCoordinator(_queueMock.Object, _connectivityMock.Object, _loggerMock.Object);

        Thread.Sleep(100);

        _queueMock.Verify(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_WhenOffline_DoesNotTriggerProcessQueue()
    {
        _connectivityMock.Setup(conn => conn.IsOffline).Returns(true);

        var coordinator = new QueueReplayCoordinator(_queueMock.Object, _connectivityMock.Object, _loggerMock.Object);

        Thread.Sleep(100);

        _queueMock.Verify(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ConnectivityChanged_TransitionsToOnline_TriggersProcessQueue()
    {
        _connectivityMock.Setup(conn => conn.IsOffline).Returns(true);
        var coordinator = new QueueReplayCoordinator(_queueMock.Object, _connectivityMock.Object, _loggerMock.Object);

        _connectivityMock.Setup(conn => conn.IsOffline).Returns(false);
        _connectivityMock.Raise(conn => conn.ConnectivityChanged += null, _connectivityMock.Object, ConnectivityStatus.Online);

        Thread.Sleep(100);

        _queueMock.Verify(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ConnectivityChanged_TransitionsToOffline_DoesNotTriggerProcessQueue()
    {
        _connectivityMock.Setup(conn => conn.IsOffline).Returns(true);
        var coordinator = new QueueReplayCoordinator(_queueMock.Object, _connectivityMock.Object, _loggerMock.Object);

        _connectivityMock.Raise(conn => conn.ConnectivityChanged += null, _connectivityMock.Object, ConnectivityStatus.Offline);

        Thread.Sleep(100);

        _queueMock.Verify(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ConnectivityChanged_MultipleOnlineEventsConcurrent_PreventsConcurrentProcessQueue()
    {
        var tcs = new TaskCompletionSource<bool>();
        _connectivityMock.Setup(conn => conn.IsOffline).Returns(true);
        _queueMock.Setup(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()))
                  .Returns(tcs.Task);

        var coordinator = new QueueReplayCoordinator(_queueMock.Object, _connectivityMock.Object, _loggerMock.Object);

        _connectivityMock.Setup(conn => conn.IsOffline).Returns(false);
        _connectivityMock.Raise(conn => conn.ConnectivityChanged += null, _connectivityMock.Object, ConnectivityStatus.Online);

        Thread.Sleep(50);

        _connectivityMock.Raise(conn => conn.ConnectivityChanged += null, _connectivityMock.Object, ConnectivityStatus.Online);

        Thread.Sleep(50);

        tcs.SetResult(true);
        Thread.Sleep(50);

        _queueMock.Verify(q => q.ProcessQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
