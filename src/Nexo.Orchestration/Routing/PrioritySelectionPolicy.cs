using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

/// <summary>Sorts and selects endpoints by configured priority weights.</summary>
internal sealed class PrioritySelectionPolicy : IRoutingPolicy
{
    public IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context)
    {
        if (candidates.Count <= 1)
        {
            return candidates;
        }

        var winner = candidates
            .OrderBy(endpoint => endpoint.Priority)
            .ThenBy(endpoint => endpoint.Name, StringComparer.OrdinalIgnoreCase)
            .First();

        return [winner];
    }
}
