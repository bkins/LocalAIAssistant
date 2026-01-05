using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.Knowledge.Tasks.Clients;

public interface ITaskApiClient
{
    Task<TasksDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}