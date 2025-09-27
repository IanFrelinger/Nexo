using System;

namespace Nexo.Feature.Pipeline.Interfaces.Requirements
{
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
}
