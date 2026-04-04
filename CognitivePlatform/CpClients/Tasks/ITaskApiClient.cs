using LocalAIAssistant.Knowledge.Tasks.Models;

namespace LocalAIAssistant.CognitivePlatform.CpClients.Tasks;

public interface ITaskApiClient
{
    Task<TasksDto?>              GetByIdAsync (Guid              id
                                             , CancellationToken ct = default);

    Task<IReadOnlyList<TasksDto>> GetAllAsync (CancellationToken ct = default);
}