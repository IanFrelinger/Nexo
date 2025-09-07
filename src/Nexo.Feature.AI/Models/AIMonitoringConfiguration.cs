using System.Text.Json.Serialization;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI monitoring and observability settings.
    /// </summary>
    public class AiMonitoringConfiguration
    {
        public AiMonitoringConfiguration(bool enableCostTracking, int healthCheckIntervalSeconds, bool enableTelemetry, string telemetryEndpoint, bool enableDistributedTracing, double tracingSamplingRate, bool enableCustomMetrics)
        {
            EnableCostTracking = enableCostTracking;
            HealthCheckIntervalSeconds = healthCheckIntervalSeconds;
            EnableTelemetry = enableTelemetry;
            TelemetryEndpoint = telemetryEndpoint;
            EnableDistributedTracing = enableDistributedTracing;
            TracingSamplingRate = tracingSamplingRate;
            EnableCustomMetrics = enableCustomMetrics;
        }

        /// <summary>
        /// Whether to enable AI monitoring.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// The monitoring data collection interval in seconds.
        /// </summary>
        [JsonPropertyName("collectionIntervalSeconds")]
        public int CollectionIntervalSeconds { get; set; } = 60;
        
        /// <summary>
        /// Whether to enable request/response logging.
        /// </summary>
        [JsonPropertyName("enableRequestResponseLogging")]
        public bool EnableRequestResponseLogging { get; set; }
        
        /// <summary>
        /// Whether to enable performance metrics collection.
        /// </summary>
        [JsonPropertyName("enablePerformanceMetrics")]
        public bool EnablePerformanceMetrics { get; set; } = true;
        
        /// <summary>
        /// Whether to enable error tracking.
        /// </summary>
        [JsonPropertyName("enableErrorTracking")]
        public bool EnableErrorTracking { get; set; } = true;
        
        /// <summary>
        /// Whether to enable usage analytics.
        /// </summary>
        [JsonPropertyName("enableUsageAnalytics")]
        public bool EnableUsageAnalytics { get; set; } = true;
        
        /// <summary>
        /// Whether to enable cost tracking.
        /// </summary>
        [JsonPropertyName("enableCostTracking")]
        public bool EnableCostTracking { get; set; }
        
        /// <summary>
        /// Whether to enable health checks.
        /// </summary>
        [JsonPropertyName("enableHealthChecks")]
        public bool EnableHealthChecks { get; set; } = true;
        
        /// <summary>
        /// The health check interval in seconds.
        /// </summary>
        [JsonPropertyName("healthCheckIntervalSeconds")]
        public int HealthCheckIntervalSeconds { get; set; } // 5 minutes
        
        /// <summary>
        /// The alert thresholds for monitoring.
        /// </summary>
        [JsonPropertyName("alertThresholds")]
        public AiAlertThresholds AlertThresholds { get; set; } = new AiAlertThresholds(1000000000, 80.0, 90.0, 0.01, 100.0, 100, 50, 0.1, 0.05);
        
        /// <summary>
        /// Whether to enable telemetry collection.
        /// </summary>
        [JsonPropertyName("enableTelemetry")]
        public bool EnableTelemetry { get; set; }
        
        /// <summary>
        /// The telemetry endpoint URL.
        /// </summary>
        [JsonPropertyName("telemetryEndpoint")]
        public string TelemetryEndpoint { get; set; }
        
        /// <summary>
        /// Whether to enable distributed tracing.
        /// </summary>
        [JsonPropertyName("enableDistributedTracing")]
        public bool EnableDistributedTracing { get; set; }
        
        /// <summary>
        /// The sampling rate for distributed tracing (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("tracingSamplingRate")]
        public double TracingSamplingRate { get; set; }
        
        /// <summary>
        /// Whether to enable custom metrics.
        /// </summary>
        [JsonPropertyName("enableCustomMetrics")]
        public bool EnableCustomMetrics { get; set; }
    }
} 