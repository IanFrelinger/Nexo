using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for managing pipeline resources and allocation.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Allocates resources for pipeline execution.
        /// </summary>
        /// <param name="requirements">The resource requirements.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Resource allocation result.</returns>
        Task<ResourceAllocation> AllocateResourcesAsync(
            ResourceRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        /// <param name="allocationId">The allocation identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the release operation.</returns>
        Task ReleaseResourcesAsync(
            string allocationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current resource utilization.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current resource utilization.</returns>
        Task<ResourceUtilization> GetCurrentUtilizationAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes resource allocation based on current usage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Resource optimization recommendations.</returns>
        Task<ResourceOptimizationRecommendation> OptimizeAllocationAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Resource requirements for pipeline execution.
    /// </summary>
    public class ResourceRequirements
    {
        /// <summary>
        /// Gets or sets the requirements identifier.
        /// </summary>
        public string RequirementsId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the CPU requirements.
        /// </summary>
        public CpuRequirements Cpu { get; set; } = new();

        /// <summary>
        /// Gets or sets the memory requirements.
        /// </summary>
        public MemoryRequirements Memory { get; set; } = new();

        /// <summary>
        /// Gets or sets the disk requirements.
        /// </summary>
        public DiskRequirements Disk { get; set; } = new();

        /// <summary>
        /// Gets or sets the network requirements.
        /// </summary>
        public NetworkRequirements Network { get; set; } = new();

        /// <summary>
        /// Gets or sets the priority level.
        /// </summary>
        public ResourcePriority Priority { get; set; } = ResourcePriority.Normal;

        /// <summary>
        /// Gets or sets the maximum allocation time.
        /// </summary>
        public TimeSpan MaxAllocationTime { get; set; } = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// CPU resource requirements.
    /// </summary>
    public class CpuRequirements
    {
        /// <summary>
        /// Gets or sets the minimum CPU cores required.
        /// </summary>
        public int MinCores { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum CPU cores required.
        /// </summary>
        public int MaxCores { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets the CPU utilization target (0-100).
        /// </summary>
        public double TargetUtilization { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets whether parallel processing is required.
        /// </summary>
        public bool RequiresParallelProcessing { get; set; } = false;
    }

    /// <summary>
    /// Memory resource requirements.
    /// </summary>
    public class MemoryRequirements
    {
        /// <summary>
        /// Gets or sets the minimum memory required in MB.
        /// </summary>
        public long MinMemoryMB { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum memory required in MB.
        /// </summary>
        public long MaxMemoryMB { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the memory growth rate (MB per second).
        /// </summary>
        public double MemoryGrowthRate { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets whether memory is critical for performance.
        /// </summary>
        public bool IsMemoryCritical { get; set; } = false;
    }

    /// <summary>
    /// Disk resource requirements.
    /// </summary>
    public class DiskRequirements
    {
        /// <summary>
        /// Gets or sets the minimum disk space required in MB.
        /// </summary>
        public long MinDiskSpaceMB { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum disk space required in MB.
        /// </summary>
        public long MaxDiskSpaceMB { get; set; } = 500;

        /// <summary>
        /// Gets or sets the required disk I/O operations per second.
        /// </summary>
        public int RequiredIOPS { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether disk access is critical for performance.
        /// </summary>
        public bool IsDiskAccessCritical { get; set; } = false;
    }

    /// <summary>
    /// Network resource requirements.
    /// </summary>
    public class NetworkRequirements
    {
        /// <summary>
        /// Gets or sets the minimum bandwidth required in Mbps.
        /// </summary>
        public double MinBandwidthMbps { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the maximum bandwidth required in Mbps.
        /// </summary>
        public double MaxBandwidthMbps { get; set; } = 100.0;

        /// <summary>
        /// Gets or sets the maximum latency tolerance in milliseconds.
        /// </summary>
        public int MaxLatencyMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether network access is critical for performance.
        /// </summary>
        public bool IsNetworkAccessCritical { get; set; } = false;
    }

    /// <summary>
    /// Resource allocation result.
    /// </summary>
    public class ResourceAllocation
    {
        /// <summary>
        /// Gets or sets the allocation identifier.
        /// </summary>
        public string AllocationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the requirements identifier.
        /// </summary>
        public string RequirementsId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allocated CPU resources.
        /// </summary>
        public AllocatedCpuResources Cpu { get; set; } = new();

        /// <summary>
        /// Gets or sets the allocated memory resources.
        /// </summary>
        public AllocatedMemoryResources Memory { get; set; } = new();

        /// <summary>
        /// Gets or sets the allocated disk resources.
        /// </summary>
        public AllocatedDiskResources Disk { get; set; } = new();

        /// <summary>
        /// Gets or sets the allocated network resources.
        /// </summary>
        public AllocatedNetworkResources Network { get; set; } = new();

        /// <summary>
        /// Gets or sets the allocation timestamp.
        /// </summary>
        public DateTime AllocationTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the allocation expiry time.
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets whether the allocation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the allocation failure reason if unsuccessful.
        /// </summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Allocated CPU resources.
    /// </summary>
    public class AllocatedCpuResources
    {
        /// <summary>
        /// Gets or sets the allocated CPU cores.
        /// </summary>
        public int AllocatedCores { get; set; }

        /// <summary>
        /// Gets or sets the CPU utilization target.
        /// </summary>
        public double TargetUtilization { get; set; }

        /// <summary>
        /// Gets or sets whether parallel processing is enabled.
        /// </summary>
        public bool ParallelProcessingEnabled { get; set; }
    }

    /// <summary>
    /// Allocated memory resources.
    /// </summary>
    public class AllocatedMemoryResources
    {
        /// <summary>
        /// Gets or sets the allocated memory in MB.
        /// </summary>
        public long AllocatedMemoryMB { get; set; }

        /// <summary>
        /// Gets or sets the memory growth rate.
        /// </summary>
        public double MemoryGrowthRate { get; set; }

        /// <summary>
        /// Gets or sets whether memory is critical.
        /// </summary>
        public bool IsMemoryCritical { get; set; }
    }

    /// <summary>
    /// Allocated disk resources.
    /// </summary>
    public class AllocatedDiskResources
    {
        /// <summary>
        /// Gets or sets the allocated disk space in MB.
        /// </summary>
        public long AllocatedDiskSpaceMB { get; set; }

        /// <summary>
        /// Gets or sets the allocated IOPS.
        /// </summary>
        public int AllocatedIOPS { get; set; }

        /// <summary>
        /// Gets or sets whether disk access is critical.
        /// </summary>
        public bool IsDiskAccessCritical { get; set; }
    }

    /// <summary>
    /// Allocated network resources.
    /// </summary>
    public class AllocatedNetworkResources
    {
        /// <summary>
        /// Gets or sets the allocated bandwidth in Mbps.
        /// </summary>
        public double AllocatedBandwidthMbps { get; set; }

        /// <summary>
        /// Gets or sets the maximum latency.
        /// </summary>
        public int MaxLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets whether network access is critical.
        /// </summary>
        public bool IsNetworkAccessCritical { get; set; }
    }

    /// <summary>
    /// Resource utilization information.
    /// </summary>
    public class ResourceUtilization
    {
        /// <summary>
        /// Gets or sets the utilization identifier.
        /// </summary>
        public string UtilizationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the CPU utilization.
        /// </summary>
        public CpuUtilization Cpu { get; set; } = new();

        /// <summary>
        /// Gets or sets the memory utilization.
        /// </summary>
        public MemoryUtilization Memory { get; set; } = new();

        /// <summary>
        /// Gets or sets the disk utilization.
        /// </summary>
        public DiskUtilization Disk { get; set; } = new();

        /// <summary>
        /// Gets or sets the network utilization.
        /// </summary>
        public NetworkUtilization Network { get; set; } = new();

        /// <summary>
        /// Gets or sets the utilization timestamp.
        /// </summary>
        public DateTime UtilizationTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// CPU utilization information.
    /// </summary>
    public class CpuUtilization
    {
        /// <summary>
        /// Gets or sets the current CPU usage percentage.
        /// </summary>
        public double CurrentUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the average CPU usage percentage.
        /// </summary>
        public double AverageUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the peak CPU usage percentage.
        /// </summary>
        public double PeakUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the number of active cores.
        /// </summary>
        public int ActiveCores { get; set; }
    }

    /// <summary>
    /// Memory utilization information.
    /// </summary>
    public class MemoryUtilization
    {
        /// <summary>
        /// Gets or sets the current memory usage in MB.
        /// </summary>
        public long CurrentUsageMB { get; set; }

        /// <summary>
        /// Gets or sets the total available memory in MB.
        /// </summary>
        public long TotalAvailableMB { get; set; }

        /// <summary>
        /// Gets or sets the memory usage percentage.
        /// </summary>
        public double UsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the memory growth rate.
        /// </summary>
        public double GrowthRate { get; set; }
    }

    /// <summary>
    /// Disk utilization information.
    /// </summary>
    public class DiskUtilization
    {
        /// <summary>
        /// Gets or sets the current disk usage in MB.
        /// </summary>
        public long CurrentUsageMB { get; set; }

        /// <summary>
        /// Gets or sets the total available disk space in MB.
        /// </summary>
        public long TotalAvailableMB { get; set; }

        /// <summary>
        /// Gets or sets the disk usage percentage.
        /// </summary>
        public double UsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the current IOPS.
        /// </summary>
        public int CurrentIOPS { get; set; }
    }

    /// <summary>
    /// Network utilization information.
    /// </summary>
    public class NetworkUtilization
    {
        /// <summary>
        /// Gets or sets the current bandwidth usage in Mbps.
        /// </summary>
        public double CurrentBandwidthMbps { get; set; }

        /// <summary>
        /// Gets or sets the total available bandwidth in Mbps.
        /// </summary>
        public double TotalAvailableBandwidthMbps { get; set; }

        /// <summary>
        /// Gets or sets the bandwidth usage percentage.
        /// </summary>
        public double UsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets the current latency in milliseconds.
        /// </summary>
        public int CurrentLatencyMs { get; set; }
    }

    /// <summary>
    /// Resource optimization recommendation.
    /// </summary>
    public class ResourceOptimizationRecommendation
    {
        /// <summary>
        /// Gets or sets the recommendation identifier.
        /// </summary>
        public string RecommendationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the optimization type.
        /// </summary>
        public OptimizationType Type { get; set; }

        /// <summary>
        /// Gets or sets the recommendation description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected improvement percentage.
        /// </summary>
        public double ExpectedImprovementPercentage { get; set; }

        /// <summary>
        /// Gets or sets the implementation complexity.
        /// </summary>
        public ImplementationComplexity ImplementationComplexity { get; set; }

        /// <summary>
        /// Gets or sets the target resource type.
        /// </summary>
        public ResourceType TargetResourceType { get; set; }
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
    /// Resource types.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// CPU resource.
        /// </summary>
        Cpu,

        /// <summary>
        /// Memory resource.
        /// </summary>
        Memory,

        /// <summary>
        /// Disk resource.
        /// </summary>
        Disk,

        /// <summary>
        /// Network resource.
        /// </summary>
        Network
    }
}
