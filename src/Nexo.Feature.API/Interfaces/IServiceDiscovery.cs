using Nexo.Feature.API.Models;

namespace Nexo.Feature.API.Interfaces;

/// <summary>
/// Service discovery interface for dynamic service registration and discovery
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Registers a new service with the discovery system
    /// </summary>
    /// <param name="service">Service information to register</param>
    /// <returns>Registration result</returns>
    Task<ServiceRegistrationResult> RegisterServiceAsync(ServiceInfo service);

    /// <summary>
    /// Unregisters a service from the discovery system
    /// </summary>
    /// <param name="serviceId">Unique identifier of the service</param>
    /// <returns>Unregistration result</returns>
    Task<ServiceUnregistrationResult> UnregisterServiceAsync(string serviceId);

    /// <summary>
    /// Discovers available services based on criteria
    /// </summary>
    /// <param name="criteria">Discovery criteria</param>
    /// <returns>Collection of matching services</returns>
    Task<IEnumerable<ServiceInfo>> DiscoverServicesAsync(ServiceDiscoveryCriteria criteria);

    /// <summary>
    /// Gets a specific service by its identifier
    /// </summary>
    /// <param name="serviceId">Unique identifier of the service</param>
    /// <returns>Service information if found</returns>
    Task<ServiceInfo?> GetServiceAsync(string serviceId);

    /// <summary>
    /// Updates service health status
    /// </summary>
    /// <param name="serviceId">Unique identifier of the service</param>
    /// <param name="healthStatus">Current health status</param>
    /// <returns>Update result</returns>
    Task<ServiceHealthUpdateResult> UpdateServiceHealthAsync(string serviceId, ServiceHealthStatus healthStatus);

    /// <summary>
    /// Gets all registered services
    /// </summary>
    /// <returns>Collection of all registered services</returns>
    Task<IEnumerable<ServiceInfo>> GetAllServicesAsync();

    /// <summary>
    /// Performs health check on all registered services
    /// </summary>
    /// <returns>Collection of service health statuses</returns>
    Task<IEnumerable<ServiceHealthStatus>> PerformHealthCheckAsync();
} 