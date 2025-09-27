using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Execution
{
    /// <summary>
    /// Represents pipeline execution settings.
    /// </summary>
    public class PipelineExecutionSettings
    {
        /// <summary>
        /// Gets or sets the maximum parallel executions.
        /// </summary>
        [JsonPropertyName("maxParallelExecutions")]
        public int MaxParallelExecutions { get; set; } = 4;

        /// <summary>
        /// Gets or sets the command timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("commandTimeoutMs")]
        public int CommandTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the behavior timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("behaviorTimeoutMs")]
        public int BehaviorTimeoutMs { get; set; } = 60000;

        /// <summary>
        /// Gets or sets the aggregator timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("aggregatorTimeoutMs")]
        public int AggregatorTimeoutMs { get; set; } = 120000;

        /// <summary>
        /// Gets or sets the maximum retries.
        /// </summary>
        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry delay in milliseconds.
        /// </summary>
        [JsonPropertyName("retryDelayMs")]
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable detailed logging.
        /// </summary>
        [JsonPropertyName("enableDetailedLogging")]
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance monitoring.
        /// </summary>
        [JsonPropertyName("enablePerformanceMonitoring")]
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable execution history.
        /// </summary>
        [JsonPropertyName("enableExecutionHistory")]
        public bool EnableExecutionHistory { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum execution history entries.
        /// </summary>
        [JsonPropertyName("maxExecutionHistoryEntries")]
        public int MaxExecutionHistoryEntries { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to enable parallel execution.
        /// </summary>
        [JsonPropertyName("enableParallelExecution")]
        public bool EnableParallelExecution { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable dependency resolution.
        /// </summary>
        [JsonPropertyName("enableDependencyResolution")]
        public bool EnableDependencyResolution { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable resource management.
        /// </summary>
        [JsonPropertyName("enableResourceManagement")]
        public bool EnableResourceManagement { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum memory usage in bytes.
        /// </summary>
        [JsonPropertyName("maxMemoryUsageBytes")]
        public long MaxMemoryUsageBytes { get; set; } = 1024 * 1024 * 1024; // 1GB

        /// <summary>
        /// Gets or sets the maximum CPU usage percentage.
        /// </summary>
        [JsonPropertyName("maxCpuUsagePercentage")]
        public double MaxCpuUsagePercentage { get; set; } = 90.0;
    }
}
