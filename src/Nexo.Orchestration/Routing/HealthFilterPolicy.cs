using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

internal sealed class HealthFilterPolicy : IRoutingPolicy
{
    public IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var healthy = candidates.Where(endpoint => endpoint.IsHealthy).ToList();
        if (healthy.Count > 0)
        {
            return healthy;
        }

        var unhealthyEndpoints = candidates
            .Select(endpoint => endpoint.Endpoint)
            .Where(endpoint => !string.IsNullOrWhiteSpace(endpoint))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(endpoint => endpoint, StringComparer.OrdinalIgnoreCase)
            .ToList();

        throw new EndpointUnavailableException(
            unhealthyEndpoints,
            $"All candidate endpoints are unhealthy for agent {context.AgentName}.");
    }
}
