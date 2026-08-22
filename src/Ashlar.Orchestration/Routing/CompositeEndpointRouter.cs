using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Routing;

namespace Ashlar.Orchestration.Routing;

/// <summary>
/// Composes routing policies into an endpoint selection pipeline.
/// </summary>
internal sealed class CompositeEndpointRouter : IEndpointRouter
{
    private readonly IEndpointRegistry _registry;
    private readonly IReadOnlyList<IRoutingPolicy> _policies;
    private readonly ILogger<CompositeEndpointRouter> _logger;

    public CompositeEndpointRouter(
        IEndpointRegistry registry,
        IEnumerable<IRoutingPolicy> policies,
        ILogger<CompositeEndpointRouter> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _policies = policies?.ToList() ?? throw new ArgumentNullException(nameof(policies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask<string?> ResolveAsync(
        EndpointRoutingContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var candidates = _registry.GetAll();
        if (candidates.Count == 0)
        {
            _logger.LogDebug(
                "Routing resolved in-process for agent {AgentName} (CorrelationId: {CorrelationId}): no endpoints registered.",
                context.AgentName,
                context.CorrelationId);
            return ValueTask.FromResult<string?>(null);
        }

        var current = candidates;
        foreach (var policy in _policies)
        {
            current = policy.Apply(current, context);
        }

        var selected = current.Count == 0 ? null : current[0];

        _logger.LogDebug(
            "Routing decision for agent {AgentName} (CorrelationId: {CorrelationId}) -> {Endpoint}.",
            context.AgentName,
            context.CorrelationId,
            selected?.Endpoint ?? "in-process");

        return ValueTask.FromResult<string?>(selected?.Endpoint);
    }
}
