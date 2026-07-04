using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

/// <summary>Routing policy contract for filtering endpoint candidates.</summary>
internal interface IRoutingPolicy
{
    IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context);
}
