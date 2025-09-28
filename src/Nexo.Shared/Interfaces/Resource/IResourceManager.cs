using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Shared.Models.Resource;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Interface for intelligent resource allocation and management.
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface IResourceManager
    {
        /// <summary>
        /// Allocates resources for a specific request.
        /// </summary>
        /// <param name="request">The resource allocation request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resource allocation result.</returns>
        Task<ResourceAllocationResult> AllocateAsync(ResourceAllocationRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        /// <param name="allocationId">The allocation ID to release.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ReleaseAsync(string allocationId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets current resource usage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Current resource usage information.</returns>
        Task<ResourceUsage> GetUsageAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets resource limits and constraints.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource limits information.</returns>
        Task<ResourceLimits> GetLimitsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Monitors resource usage and provides alerts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource monitoring information.</returns>
        Task<ResourceMonitoringInfo> MonitorAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Optimizes resource allocation based on current usage patterns.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource optimization recommendations.</returns>
        Task<ResourceOptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Registers a resource provider.
        /// </summary>
        /// <param name="provider">The resource provider to register.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RegisterProviderAsync(IResourceProvider provider, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Unregisters a resource provider.
        /// </summary>
        /// <param name="providerId">The provider ID to unregister.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UnregisterProviderAsync(string providerId, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Interface for resource providers.
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface IResourceProvider
    {
        /// <summary>
        /// Gets the provider ID.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the resource types supported by this provider.
        /// </summary>
        IEnumerable<ResourceType> SupportedResourceTypes { get; }

        /// <summary>
        /// Gets the current resource availability.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource availability information.</returns>
        Task<ResourceAvailability> GetAvailabilityAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Allocates resources from this provider.
        /// </summary>
        /// <param name="request">The allocation request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The allocation result.</returns>
        Task<ResourceAllocationResult> AllocateAsync(ResourceAllocationRequest request, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Releases resources back to this provider.
        /// </summary>
        /// <param name="allocationId">The allocation ID to release.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ReleaseAsync(string allocationId, CancellationToken cancellationToken = default(CancellationToken));
    }
    // This interface acts as an orchestrator for various resource management functionalities,
    // with specific categories defined in partial interfaces.
}
