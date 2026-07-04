namespace Nexo.Abstractions.Routing;

/// <summary>
/// Registry for endpoint descriptors and their health state.
/// </summary>
public interface IEndpointRegistry
{
    /// <summary>Returns all registered endpoint descriptors.</summary>
    IReadOnlyList<EndpointDescriptor> GetAll();

    /// <summary>Registers or replaces an endpoint descriptor in the registry.</summary>
    /// <param name="descriptor">Endpoint metadata used by routing policies.</param>
    void Register(EndpointDescriptor descriptor);

    /// <summary>Updates the health flag for a registered endpoint.</summary>
    /// <param name="endpoint">Endpoint address or identifier.</param>
    /// <param name="isHealthy">Whether the endpoint is currently reachable.</param>
    void UpdateHealth(string endpoint, bool isHealthy);
}
