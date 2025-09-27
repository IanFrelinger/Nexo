using System;

namespace Nexo.Feature.Pipeline.Interfaces.Allocation
{
    /// <summary>
    /// Resource allocation result.
    /// </summary>
    public partial class ResourceAllocation
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
    public partial class AllocatedCpuResources
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
    public partial class AllocatedMemoryResources
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
    public partial class AllocatedDiskResources
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
    public partial class AllocatedNetworkResources
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
}
