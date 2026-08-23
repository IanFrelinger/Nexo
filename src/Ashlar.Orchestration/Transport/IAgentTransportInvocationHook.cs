using Ashlar.Abstractions.Transport;

namespace Ashlar.Orchestration.Transport;

/// <summary>
/// Extensibility point around transport-level agent invocation. Register multiple
/// implementations in DI; <see cref="BeforeSendAsync"/> runs in registration order,
/// then <see cref="AfterSuccessAsync"/> runs in registration order on successful results.
/// </summary>
public interface IAgentTransportInvocationHook
{
    /// <summary>
    /// Transform or enrich the invocation before it is passed to retry/circuit-breaker and transport.
    /// </summary>
    Task<AgentInvocationRequest> BeforeSendAsync(
        AgentInvocationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(context.Request);

    /// <summary>
    /// Optional post-processing of the successful output payload before dependency recording and bus publish.
    /// </summary>
    Task<object> AfterSuccessAsync(
        AgentInvocationContext context,
        object output,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(output);
}
