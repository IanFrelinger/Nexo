namespace Nexo.Abstractions.Routing;

/// <summary>
/// Registry for endpoint descriptors and their health state.
/// </summary>
public interface IEndpointRegistry
{
    IReadOnlyList<EndpointDescriptor> GetAll();

    void Register(EndpointDescriptor descriptor);

    void UpdateHealth(string endpoint, bool isHealthy);
}
