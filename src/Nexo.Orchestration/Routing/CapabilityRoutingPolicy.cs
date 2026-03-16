using Nexo.Abstractions.Routing;

namespace Nexo.Orchestration.Routing;

internal sealed class CapabilityRoutingPolicy : IRoutingPolicy
{
    public IReadOnlyList<EndpointDescriptor> Apply(
        IReadOnlyList<EndpointDescriptor> candidates,
        EndpointRoutingContext context)
    {
        if (context.RequiredCapabilities.Count == 0)
        {
            return candidates;
        }

        return candidates
            .Where(endpoint => SupportsAll(endpoint.SupportedCapabilities, context.RequiredCapabilities))
            .ToList();
    }

    private static bool SupportsAll(
        IReadOnlyList<string> supportedCapabilities,
        IReadOnlyList<string> requiredCapabilities)
    {
        var supported = new HashSet<string>(supportedCapabilities, StringComparer.OrdinalIgnoreCase);
        return requiredCapabilities.All(capability => supported.Contains(capability));
    }
}
