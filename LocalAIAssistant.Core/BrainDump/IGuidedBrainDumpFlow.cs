namespace LocalAIAssistant.Core.BrainDump;

public interface IGuidedBrainDumpFlow
{
    bool IsActive { get; }

    bool IsTrigger(string input);

    Task<FlowTurn> StartAsync   (Func<string, CancellationToken, Task<string>> converseFn
                                , CancellationToken                             ct = default);

    Task<FlowTurn> HandleInputAsync(string input, CancellationToken ct = default);

    void Reset();
}
