using System.Net;
using System.Text;
using System.Text.Json;
using LocalAIAssistant.CognitivePlatform.CpClients.Tasks;
using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LaaUnitTests;

public class TaskApiClientTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsTask_WhenFound()
    {
        var id   = Guid.NewGuid();
        var task = new TasksDto
        {
            Id               = id.ToString()
          , ShortDescription = "Buy groceries"
          , IsImportant      = true
          , IsUrgent         = false
        };
        var json = JsonSerializer.Serialize(task);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(task.Id,         result!.Id);
        Assert.Equal("Buy groceries", result.ShortDescription);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var sut    = BuildClient(HttpStatusCode.NotFound, string.Empty);
        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTasksList_WhenSuccessful()
    {
        var tasks = new List<TasksDto>
        {
            new() { Id = Guid.NewGuid().ToString(), ShortDescription = "Task 1" },
            new() { Id = Guid.NewGuid().ToString(), ShortDescription = "Task 2" }
        };
        var json = JsonSerializer.Serialize(tasks);

        var sut    = BuildClient(HttpStatusCode.OK, json);
        var result = await sut.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNetworkFails()
    {
        var sut    = BuildThrowingClient();
        var result = await sut.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task EditTaskAsync_SendsCorrectPayload_WhenSuccessful()
    {
        var taskId = Guid.NewGuid();
        var sut    = BuildClient(HttpStatusCode.OK, "{}");

        var exception = await Record.ExceptionAsync(() => sut.EditTaskAsync(
            taskId,
            "Updated task description",
            "Updated details",
            ["tag1", "tag2"],
            DateTimeOffset.UtcNow.AddDays(1),
            null));

        Assert.Null(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TaskApiClient BuildClient(HttpStatusCode status, string content)
    {
        var handler = new StubHttpMessageHandler(status, content);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new TaskApiClient(client);
    }

    private static TaskApiClient BuildThrowingClient()
    {
        var client = new HttpClient(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
        return new TaskApiClient(client);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string         _content;

        public StubHttpMessageHandler(HttpStatusCode status, string content)
        {
            _status  = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Simulated network failure");
    }
}
