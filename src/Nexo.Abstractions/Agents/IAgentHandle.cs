namespace Nexo.Abstractions.Agents;

/// <summary>
/// Runtime handle for a spawned orchestration agent (composition root over in-process or remote execution).
/// </summary>
public interface IAgentHandle
{
    string AgentId { get; }
    AgentState State { get; }
    AgentHealth Health { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<object> ExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default);

    Task ShutdownAsync(CancellationToken cancellationToken = default);

    void Terminate();
}
