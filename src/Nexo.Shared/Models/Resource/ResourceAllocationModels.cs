using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Shared.Values;

namespace Nexo.Shared.Models.Resource
{
    /// <summary>
    /// Resource allocation request.
    /// </summary>
    public partial class ResourceAllocationRequest
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.CPU;

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

        /// <summary>
        /// Gets or sets the deadline for resource allocation.
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Gets or sets the constraints for resource allocation.
        /// </summary>
        public List<string> Constraints { get; set; } = new List<string>();
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
        /// Gets or sets the allocated resource identifier.
        /// </summary>
        public string? AllocatedResourceId { get; set; }

        /// <summary>
        /// Gets or sets the actual allocated amount.
        /// </summary>
        public long AllocatedAmount { get; set; }

        /// <summary>
        /// Gets or sets the allocation timestamp.
        /// </summary>
        public DateTime AllocatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the allocation.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets any error message if allocation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional allocation metadata.
        /// </summary>
        public Dictionary<string, object> AllocationMetadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource deallocation request.
    /// </summary>
    public partial class ResourceDeallocationRequest
    {
        /// <summary>
        /// Gets or sets the resource identifier to deallocate.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requester identifier.
        /// </summary>
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for deallocation.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource deallocation result.
    /// </summary>
    public partial class ResourceDeallocationResult
    {
        /// <summary>
        /// Gets or sets whether the deallocation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the deallocation timestamp.
        /// </summary>
        public DateTime DeallocatedAt { get; set; }

        /// <summary>
        /// Gets or sets any error message if deallocation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the amount of resources that were deallocated.
        /// </summary>
        public long DeallocatedAmount { get; set; }
    }

    /// <summary>
    /// Current resource usage information.
    /// </summary>
    public partial class ResourceUsage
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.CPU;

        /// <summary>
        /// Gets or sets the current usage amount.
        /// </summary>
        public long CurrentUsage { get; set; }

        /// <summary>
        /// Gets or sets the maximum available amount.
        /// </summary>
        public long MaxAvailable { get; set; }

        /// <summary>
        /// Gets or sets the usage percentage.
        /// </summary>
        public double UsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when usage was measured.
        /// </summary>
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the usage.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource limits and constraints.
    /// </summary>
    public partial class ResourceLimits
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.CPU;

        /// <summary>
        /// Gets or sets the maximum allowed amount.
        /// </summary>
        public long MaxLimit { get; set; }

        /// <summary>
        /// Gets or sets the minimum required amount.
        /// </summary>
        public long MinLimit { get; set; }

        /// <summary>
        /// Gets or sets the soft limit (warning threshold).
        /// </summary>
        public long SoftLimit { get; set; }

        /// <summary>
        /// Gets or sets the hard limit (critical threshold).
        /// </summary>
        public long HardLimit { get; set; }

        /// <summary>
        /// Gets or sets whether limits are enforced.
        /// </summary>
        public bool IsEnforced { get; set; } = true;

        /// <summary>
        /// Gets or sets additional metadata about the limits.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource availability information.
    /// </summary>
    public partial class ResourceAvailability
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.CPU;

        /// <summary>
        /// Gets or sets the available amount.
        /// </summary>
        public long AvailableAmount { get; set; }

        /// <summary>
        /// Gets or sets the total capacity.
        /// </summary>
        public long TotalCapacity { get; set; }

        /// <summary>
        /// Gets or sets the availability percentage.
        /// </summary>
        public double AvailabilityPercentage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when availability was measured.
        /// </summary>
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the availability.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
