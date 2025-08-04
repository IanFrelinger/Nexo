using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Interface for intelligent resource allocation and management.
    /// </summary>
    public interface IResourceManager
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
    /// </summary>
    public interface IResourceProvider
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

    /// <summary>
    /// Resource allocation request.
    /// </summary>
    public class ResourceAllocationRequest
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the requested amount.
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Gets or sets the priority level.
        /// </summary>
        public ResourcePriority Priority { get; set; } = ResourcePriority.Normal;

        /// <summary>
        /// Gets or sets the duration for which the resource is needed.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the requester identifier.
        /// </summary>
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource allocation result.
    /// </summary>
    public class ResourceAllocationResult
    {
        /// <summary>
        /// Gets or sets whether the allocation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the allocation ID.
        /// </summary>
        public string AllocationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allocated amount.
        /// </summary>
        public long AllocatedAmount { get; set; }

        /// <summary>
        /// Gets or sets the provider that allocated the resource.
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allocation timestamp.
        /// </summary>
        public DateTime AllocatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets any error message if allocation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource usage information.
    /// </summary>
    public class ResourceUsage
    {
        /// <summary>
        /// Gets or sets the total allocated resources by type.
        /// </summary>
        public Dictionary<ResourceType, long> AllocatedByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the total available resources by type.
        /// </summary>
        public Dictionary<ResourceType, long> AvailableByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the utilization percentage by type.
        /// </summary>
        public Dictionary<ResourceType, double> UtilizationByType { get; set; } = new Dictionary<ResourceType, double>();

        /// <summary>
        /// Gets or sets the active allocations.
        /// </summary>
        public List<ResourceAllocation> ActiveAllocations { get; set; } = new List<ResourceAllocation>();

        /// <summary>
        /// Gets or sets the timestamp of this usage information.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Resource limits information.
    /// </summary>
    public class ResourceLimits
    {
        /// <summary>
        /// Gets or sets the maximum resources by type.
        /// </summary>
        public Dictionary<ResourceType, long> MaximumByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the soft limits by type.
        /// </summary>
        public Dictionary<ResourceType, long> SoftLimitsByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the hard limits by type.
        /// </summary>
        public Dictionary<ResourceType, long> HardLimitsByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the allocation policies by type.
        /// </summary>
        public Dictionary<ResourceType, ResourceAllocationPolicy> PoliciesByType { get; set; } = new Dictionary<ResourceType, ResourceAllocationPolicy>();
    }

    /// <summary>
    /// Resource monitoring information.
    /// </summary>
    public class ResourceMonitoringInfo
    {
        /// <summary>
        /// Gets or sets the current alerts.
        /// </summary>
        public List<ResourceAlert> Alerts { get; set; } = new List<ResourceAlert>();

        /// <summary>
        /// Gets or sets the performance metrics.
        /// </summary>
        public Dictionary<ResourceType, ResourceMetrics> MetricsByType { get; set; } = new Dictionary<ResourceType, ResourceMetrics>();

        /// <summary>
        /// Gets or sets the health status.
        /// </summary>
        public ResourceHealthStatus HealthStatus { get; set; } = new ResourceHealthStatus();
    }

    /// <summary>
    /// Resource optimization result.
    /// </summary>
    public class ResourceOptimizationResult
    {
        /// <summary>
        /// Gets or sets whether optimization was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the optimization recommendations.
        /// </summary>
        public List<ResourceOptimizationRecommendation> Recommendations { get; set; } = new List<ResourceOptimizationRecommendation>();

        /// <summary>
        /// Gets or sets the expected improvements.
        /// </summary>
        public Dictionary<ResourceType, double> ExpectedImprovements { get; set; } = new Dictionary<ResourceType, double>();
    }

    /// <summary>
    /// Resource availability information.
    /// </summary>
    public class ResourceAvailability
    {
        /// <summary>
        /// Gets or sets the available resources by type.
        /// </summary>
        public Dictionary<ResourceType, long> AvailableByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the total resources by type.
        /// </summary>
        public Dictionary<ResourceType, long> TotalByType { get; set; } = new Dictionary<ResourceType, long>();

        /// <summary>
        /// Gets or sets the provider health status.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Active resource allocation.
    /// </summary>
    public class ResourceAllocation
    {
        /// <summary>
        /// Gets or sets the allocation ID.
        /// </summary>
        public string AllocationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the allocated amount.
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Gets or sets the requester ID.
        /// </summary>
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allocation timestamp.
        /// </summary>
        public DateTime AllocatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public ResourcePriority Priority { get; set; }
    }

    /// <summary>
    /// Resource alert.
    /// </summary>
    public class ResourceAlert
    {
        /// <summary>
        /// Gets or sets the alert ID.
        /// </summary>
        public string AlertId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the alert type.
        /// </summary>
        public ResourceAlertType Type { get; set; }

        /// <summary>
        /// Gets or sets the alert severity.
        /// </summary>
        public ResourceAlertSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the alert message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Resource metrics.
    /// </summary>
    public class ResourceMetrics
    {
        /// <summary>
        /// Gets or sets the average utilization.
        /// </summary>
        public double AverageUtilization { get; set; }

        /// <summary>
        /// Gets or sets the peak utilization.
        /// </summary>
        public double PeakUtilization { get; set; }

        /// <summary>
        /// Gets or sets the allocation count.
        /// </summary>
        public long AllocationCount { get; set; }

        /// <summary>
        /// Gets or sets the release count.
        /// </summary>
        public long ReleaseCount { get; set; }

        /// <summary>
        /// Gets or sets the average allocation time.
        /// </summary>
        public TimeSpan AverageAllocationTime { get; set; }
    }

    /// <summary>
    /// Resource health status.
    /// </summary>
    public class ResourceHealthStatus
    {
        /// <summary>
        /// Gets or sets the overall health status.
        /// </summary>
        public ResourceHealth OverallStatus { get; set; }

        /// <summary>
        /// Gets or sets the health status by resource type.
        /// </summary>
        public Dictionary<ResourceType, ResourceHealth> StatusByType { get; set; } = new Dictionary<ResourceType, ResourceHealth>();

        /// <summary>
        /// Gets or sets the last check time.
        /// </summary>
        public DateTime LastCheckTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Resource optimization recommendation.
    /// </summary>
    public class ResourceOptimizationRecommendation
    {
        /// <summary>
        /// Gets or sets the recommendation type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recommendation message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected impact.
        /// </summary>
        public string Impact { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// Resource allocation policy.
    /// </summary>
    public class ResourceAllocationPolicy
    {
        /// <summary>
        /// Gets or sets the maximum allocation per request.
        /// </summary>
        public long MaxAllocationPerRequest { get; set; }

        /// <summary>
        /// Gets or sets the minimum allocation per request.
        /// </summary>
        public long MinAllocationPerRequest { get; set; }

        /// <summary>
        /// Gets or sets the allocation timeout.
        /// </summary>
        public TimeSpan AllocationTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether to allow over-allocation.
        /// </summary>
        public bool AllowOverAllocation { get; set; }

        /// <summary>
        /// Gets or sets the over-allocation limit percentage.
        /// </summary>
        public double OverAllocationLimitPercentage { get; set; }
    }

    /// <summary>
    /// Resource types.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// CPU resources.
        /// </summary>
        CPU,

        /// <summary>
        /// Memory resources.
        /// </summary>
        Memory,

        /// <summary>
        /// GPU resources.
        /// </summary>
        GPU,

        /// <summary>
        /// Storage resources.
        /// </summary>
        Storage,

        /// <summary>
        /// Network bandwidth.
        /// </summary>
        Network,

        /// <summary>
        /// AI model resources.
        /// </summary>
        AIModel
    }

    /// <summary>
    /// Resource priority levels.
    /// </summary>
    public enum ResourcePriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Resource alert types.
    /// </summary>
    public enum ResourceAlertType
    {
        /// <summary>
        /// High utilization alert.
        /// </summary>
        HighUtilization,

        /// <summary>
        /// Resource exhaustion alert.
        /// </summary>
        ResourceExhaustion,

        /// <summary>
        /// Allocation failure alert.
        /// </summary>
        AllocationFailure,

        /// <summary>
        /// Provider health alert.
        /// </summary>
        ProviderHealth
    }

    /// <summary>
    /// Resource alert severity levels.
    /// </summary>
    public enum ResourceAlertSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Information,

        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Resource health levels.
    /// </summary>
    public enum ResourceHealth
    {
        /// <summary>
        /// Healthy status.
        /// </summary>
        Healthy,

        /// <summary>
        /// Degraded status.
        /// </summary>
        Degraded,

        /// <summary>
        /// Unhealthy status.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Unknown status.
        /// </summary>
        Unknown
    }
} 