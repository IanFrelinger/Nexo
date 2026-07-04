using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

/// <summary>Prefers endpoints in the requested geographic region when available.</summary>
internal sealed class GeographicAffinityPolicy : IRoutingPolicy
{
    private readonly ILogger<GeographicAffinityPolicy> _logger;

    public GeographicAffinityPolicy(ILogger<GeographicAffinityPolicy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PreferredRegion))
        {
            return candidates;
        }

        var regionMatches = candidates
            .Where(endpoint => string.Equals(endpoint.Region, context.PreferredRegion, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (regionMatches.Count > 0)
        {
            return regionMatches;
        }

        _logger.LogWarning(
            "No endpoint matched preferred region {Region} for agent {AgentName} (CorrelationId: {CorrelationId}); falling back to non-regional candidates.",
            context.PreferredRegion,
            context.AgentName,
            context.CorrelationId);

        return candidates;
    }
}
