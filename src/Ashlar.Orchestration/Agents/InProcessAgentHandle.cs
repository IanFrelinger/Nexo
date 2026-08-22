using Ashlar.Abstractions.Agents;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// In-process orchestration agent handle backed by <see cref="AgentContainer"/>.
/// </summary>
public sealed class InProcessAgentHandle : IAgentHandle
{
    private readonly AgentContainer _container;

    internal AgentContainer Container => _container;

    public InProcessAgentHandle(AgentContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public string AgentId => _container.AgentId;

    public AgentState State => _container.State;

    public AgentHealth Health => _container.Health;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _container.InitializeAsync(cancellationToken);

    public Task<object> ExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default) =>
        _container.ExecuteAsync(dependencyOutputs, cancellationToken);

    public Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        _container.ShutdownAsync(cancellationToken);

    public void Terminate() => _container.Terminate();
}
