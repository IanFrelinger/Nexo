using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

/// <summary>Filters endpoints by accepted barrier levels in the routing context.</summary>
internal sealed class BarrierRoutingPolicy : IRoutingPolicy
{
    public IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context)
    {
        return candidates
            .Where(endpoint =>
                endpoint.AcceptedBarrierLevels.Count == 0 ||
                endpoint.AcceptedBarrierLevels.Contains(context.BarrierLevel, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
