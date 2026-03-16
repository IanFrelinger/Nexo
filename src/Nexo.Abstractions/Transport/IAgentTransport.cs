namespace Nexo.Abstractions.Transport;

/// <summary>
/// Contract for invoking an agent over an execution transport.
/// Implementations may execute in-process, over RPC, or via message queues.
/// </summary>
public interface IAgentTransport
{
    /// <summary>
    /// Sends an agent invocation request and awaits the result.
    /// </summary>
    /// <param name="request">Invocation payload and correlation metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent invocation result.</returns>
    Task<AgentResult> SendAsync(
        AgentInvocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks health of the transport channel itself (not agent business health).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transport health status.</returns>
    Task<TransportHealth> CheckHealthAsync(CancellationToken cancellationToken = default);
}
