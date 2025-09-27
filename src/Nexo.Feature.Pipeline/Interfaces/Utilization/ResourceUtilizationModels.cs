using System;

namespace Nexo.Feature.Pipeline.Interfaces.Utilization
{
    /// <summary>
    /// Resource utilization information.
    /// </summary>
    public partial class ResourceUtilization
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
    public partial class CpuUtilization
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
    public partial class MemoryUtilization
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
    public partial class DiskUtilization
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
    public partial class NetworkUtilization
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
}
