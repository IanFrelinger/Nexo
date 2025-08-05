using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI resource allocation and management.
    /// </summary>
    public class AiResourceConfiguration
    {
        public AiResourceConfiguration(long maxNetworkBandwidthBytesPerSecond, long maxDiskUsageBytes, ResourceAllocationStrategy allocationStrategy, bool enableAutoScaling, double minResourceAllocationPercentage, double maxResourceAllocationPercentage)
        {
            MaxNetworkBandwidthBytesPerSecond = maxNetworkBandwidthBytesPerSecond;
            MaxDiskUsageBytes = maxDiskUsageBytes;
            AllocationStrategy = allocationStrategy;
            EnableAutoScaling = enableAutoScaling;
            MinResourceAllocationPercentage = minResourceAllocationPercentage;
            MaxResourceAllocationPercentage = maxResourceAllocationPercentage;
        }

        /// <summary>
        /// The maximum number of concurrent AI requests.
        /// </summary>
        [JsonPropertyName("maxConcurrentRequests")]
        public int MaxConcurrentRequests { get; set; } = 10;
        
        /// <summary>
        /// The maximum memory usage in bytes.
        /// </summary>
        [JsonPropertyName("maxMemoryUsageBytes")]
        public long MaxMemoryUsageBytes { get; set; } = 2L * 1024L * 1024L * 1024L; // 2GB
        
        /// <summary>
        /// The maximum CPU usage percentage.
        /// </summary>
        [JsonPropertyName("maxCpuUsagePercentage")]
        public double MaxCpuUsagePercentage { get; set; } = 80.0;
        
        /// <summary>
        /// The maximum GPU memory usage in bytes.
        /// </summary>
        [JsonPropertyName("maxGpuMemoryUsageBytes")]
        public long MaxGpuMemoryUsageBytes { get; set; } = 4L * 1024L * 1024L * 1024L; // 4GB
        
        /// <summary>
        /// The maximum GPU usage percentage.
        /// </summary>
        [JsonPropertyName("maxGpuUsagePercentage")]
        public double MaxGpuUsagePercentage { get; set; } = 90.0;
        
        /// <summary>
        /// The maximum network bandwidth in bytes per second.
        /// </summary>
        [JsonPropertyName("maxNetworkBandwidthBytesPerSecond")]
        public long MaxNetworkBandwidthBytesPerSecond { get; set; } // 10MB/s
        
        /// <summary>
        /// The maximum disk usage in bytes.
        /// </summary>
        [JsonPropertyName("maxDiskUsageBytes")]
        public long MaxDiskUsageBytes { get; set; } // 10GB
        
        /// <summary>
        /// The resource allocation strategy.
        /// </summary>
        [JsonPropertyName("allocationStrategy")]
        public ResourceAllocationStrategy AllocationStrategy { get; set; }
        
        /// <summary>
        /// Whether to enable resource monitoring.
        /// </summary>
        [JsonPropertyName("enableResourceMonitoring")]
        public bool EnableResourceMonitoring { get; set; } = true;
        
        /// <summary>
        /// The resource monitoring interval in seconds.
        /// </summary>
        [JsonPropertyName("resourceMonitoringIntervalSeconds")]
        public int ResourceMonitoringIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// Whether to enable automatic resource scaling.
        /// </summary>
        [JsonPropertyName("enableAutoScaling")]
        public bool EnableAutoScaling { get; set; }
        
        /// <summary>
        /// The minimum resource allocation percentage.
        /// </summary>
        [JsonPropertyName("minResourceAllocationPercentage")]
        public double MinResourceAllocationPercentage { get; set; }
        
        /// <summary>
        /// The maximum resource allocation percentage.
        /// </summary>
        [JsonPropertyName("maxResourceAllocationPercentage")]
        public double MaxResourceAllocationPercentage { get; set; }
    }
} 