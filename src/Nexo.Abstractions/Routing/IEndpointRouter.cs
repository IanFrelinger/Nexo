namespace Nexo.Abstractions.Routing;

/// <summary>
/// Resolves invocation endpoints from routing context and policy.
/// </summary>
public interface IEndpointRouter
{
    /// <summary>
    /// Resolve the target endpoint for a given agent invocation context.
    /// Returns null for in-process execution.
    /// </summary>
    ValueTask<string?> ResolveAsync(
        EndpointRoutingContext context,
        CancellationToken cancellationToken = default);
}
