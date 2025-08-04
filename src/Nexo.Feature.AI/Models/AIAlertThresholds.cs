using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Defines alert thresholds for AI monitoring.
    /// </summary>
    public class AIAlertThresholds
    {
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
        public long MaxMemoryUsageBytes { get; set; } = 1L * 1024L * 1024L * 1024L; // 1GB
        
        /// <summary>
        /// The maximum CPU usage threshold percentage.
        /// </summary>
        [JsonPropertyName("maxCpuUsagePercentage")]
        public double MaxCpuUsagePercentage { get; set; } = 80.0;
        
        /// <summary>
        /// The maximum GPU usage threshold percentage.
        /// </summary>
        [JsonPropertyName("maxGpuUsagePercentage")]
        public double MaxGpuUsagePercentage { get; set; } = 90.0;
        
        /// <summary>
        /// The maximum cost per request threshold in dollars.
        /// </summary>
        [JsonPropertyName("maxCostPerRequest")]
        public double MaxCostPerRequest { get; set; } = 0.10; // $0.10
        
        /// <summary>
        /// The maximum daily cost threshold in dollars.
        /// </summary>
        [JsonPropertyName("maxDailyCost")]
        public double MaxDailyCost { get; set; } = 10.0; // $10.00
        
        /// <summary>
        /// The maximum concurrent requests threshold.
        /// </summary>
        [JsonPropertyName("maxConcurrentRequests")]
        public int MaxConcurrentRequests { get; set; } = 50;
        
        /// <summary>
        /// The maximum queue length threshold.
        /// </summary>
        [JsonPropertyName("maxQueueLength")]
        public int MaxQueueLength { get; set; } = 100;
        
        /// <summary>
        /// The maximum cache miss rate threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("maxCacheMissRate")]
        public double MaxCacheMissRate { get; set; } = 0.3; // 30%
        
        /// <summary>
        /// The maximum fallback rate threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("maxFallbackRate")]
        public double MaxFallbackRate { get; set; } = 0.1; // 10%
        
        /// <summary>
        /// Custom alert thresholds.
        /// </summary>
        [JsonPropertyName("customThresholds")]
        public Dictionary<string, object> CustomThresholds { get; set; } = new Dictionary<string, object>();
    }
} 