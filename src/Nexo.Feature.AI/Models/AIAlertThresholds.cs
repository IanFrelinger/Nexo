using System.Text.Json.Serialization;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Defines alert thresholds for AI monitoring.
    /// </summary>
    public class AiAlertThresholds
    {
        public AiAlertThresholds(long maxMemoryUsageBytes, double maxCpuUsagePercentage, double maxGpuUsagePercentage, double maxCostPerRequest, double maxDailyCost, int maxConcurrentRequests, int maxQueueLength, double maxCacheMissRate, double maxFallbackRate)
        {
            MaxMemoryUsageBytes = maxMemoryUsageBytes;
            MaxCpuUsagePercentage = maxCpuUsagePercentage;
            MaxGpuUsagePercentage = maxGpuUsagePercentage;
            MaxCostPerRequest = maxCostPerRequest;
            MaxDailyCost = maxDailyCost;
            MaxConcurrentRequests = maxConcurrentRequests;
            MaxQueueLength = maxQueueLength;
            MaxCacheMissRate = maxCacheMissRate;
            MaxFallbackRate = maxFallbackRate;
        }

        /// <summary>
        /// The maximum response time threshold in milliseconds.
        /// </summary>
        [JsonPropertyName("maxResponseTimeMs")]
        public int MaxResponseTimeMs { get; set; } = 10000;
        
        /// <summary>
        /// The maximum error rate threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("maxErrorRate")]
        public double MaxErrorRate { get; set; } = 0.05; // 5%
        
        /// <summary>
        /// The maximum memory usage threshold in bytes.
        /// </summary>
        [JsonPropertyName("maxMemoryUsageBytes")]
        public long MaxMemoryUsageBytes { get; set; } // 1GB
        
        /// <summary>
        /// The maximum CPU usage threshold percentage.
        /// </summary>
        [JsonPropertyName("maxCpuUsagePercentage")]
        public double MaxCpuUsagePercentage { get; set; }
        
        /// <summary>
        /// The maximum GPU usage threshold percentage.
        /// </summary>
        [JsonPropertyName("maxGpuUsagePercentage")]
        public double MaxGpuUsagePercentage { get; set; }
        
        /// <summary>
        /// The maximum cost per request threshold in dollars.
        /// </summary>
        [JsonPropertyName("maxCostPerRequest")]
        public double MaxCostPerRequest { get; set; } // $0.10
        
        /// <summary>
        /// The maximum daily cost threshold in dollars.
        /// </summary>
        [JsonPropertyName("maxDailyCost")]
        public double MaxDailyCost { get; set; } // $10.00
        
        /// <summary>
        /// The maximum concurrent requests threshold.
        /// </summary>
        [JsonPropertyName("maxConcurrentRequests")]
        public int MaxConcurrentRequests { get; set; }
        
        /// <summary>
        /// The maximum queue length threshold.
        /// </summary>
        [JsonPropertyName("maxQueueLength")]
        public int MaxQueueLength { get; set; }
        
        /// <summary>
        /// The maximum cache miss rate threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("maxCacheMissRate")]
        public double MaxCacheMissRate { get; set; } // 30%
        
        /// <summary>
        /// The maximum fallback rate threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("maxFallbackRate")]
        public double MaxFallbackRate { get; set; } // 10%
    }
} 