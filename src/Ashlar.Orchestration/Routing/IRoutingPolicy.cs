using Ashlar.Abstractions.Routing;

namespace Ashlar.Orchestration.Routing;

/// <summary>Routing policy contract for filtering endpoint candidates.</summary>
internal interface IRoutingPolicy
{
    IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context);
}
