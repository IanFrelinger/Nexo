using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

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
