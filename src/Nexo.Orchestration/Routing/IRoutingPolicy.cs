using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

internal interface IRoutingPolicy
{
    IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context);
}
