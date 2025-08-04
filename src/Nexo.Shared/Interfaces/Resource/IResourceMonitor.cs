using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Shared.Interfaces.Resource
{
    /// <summary>
    /// Defines the contract for monitoring system resources.
    /// </summary>
    public interface IResourceMonitor
    {
        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        Task<double> GetCpuUsageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current memory usage information.
        /// </summary>
        Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current disk usage information for a specific path.
        /// </summary>
        Task<DiskInfo> GetDiskInfoAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive resource usage information.
        /// </summary>
        Task<SystemResourceUsage> GetResourceUsageAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents memory usage information.
    /// </summary>
    public class MemoryInfo
    {
        public long TotalBytes { get; set; }
        public long AvailableBytes { get; set; }
        public long UsedBytes => TotalBytes - AvailableBytes;
        public double UsagePercentage => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    }

    /// <summary>
    /// Represents disk usage information.
    /// </summary>
    public class DiskInfo
    {
        public long TotalBytes { get; set; }
        public long AvailableBytes { get; set; }
        public long UsedBytes => TotalBytes - AvailableBytes;
        public double UsagePercentage => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents comprehensive system resource usage information.
    /// </summary>
    public class SystemResourceUsage
    {
        public double CpuUsagePercentage { get; set; }
        public MemoryInfo Memory { get; set; } = new MemoryInfo();
        public DiskInfo Disk { get; set; } = new DiskInfo();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 