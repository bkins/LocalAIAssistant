using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

public interface ITaskApiClient
{
    Task<TasksDto?>              GetByIdAsync (Guid              id
                                             , CancellationToken ct = default);

    Task<IReadOnlyList<TasksDto>> GetAllAsync (CancellationToken ct = default);

    Task EditTaskAsync(Guid                    taskId
                     , string                  shortDescription
                     , string?                 details
                     , IReadOnlyList<string>?  tags
                     , DateTimeOffset?         dueDate
                     , DateTimeOffset?         completedAt
                     , CancellationToken       ct = default);
}