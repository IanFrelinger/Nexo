using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Model definitions for IResourceManager.
    /// </summary>
    public partial interface IResourceManager
    {
        // This interface acts as an orchestrator for various resource management functionalities,
        // with specific categories defined in partial interfaces.
    }

    /// <summary>
    /// Resource allocation request.
    /// </summary>
    public partial class ResourceAllocationRequest
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
    public partial class ResourceAllocationResult
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
    public partial class ResourceUsage
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
    public partial class ResourceLimits
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
    public partial class ResourceMonitoringInfo
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
    public partial class ResourceOptimizationResult
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
    public partial class ResourceAvailability
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
    public partial class ResourceAllocation
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
    public partial class ResourceAlert
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
    public partial class ResourceMetrics
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
    public partial class ResourceHealthStatus
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
    public partial class ResourceOptimizationRecommendation
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
    public partial class ResourceAllocationPolicy
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
}
