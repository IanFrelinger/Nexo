using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Configuration interface for pipeline execution.
    /// </summary>
    public interface IPipelineConfiguration
    {
        /// <summary>
        /// Maximum number of parallel executions allowed.
        /// </summary>
        int MaxParallelExecutions { get; }
        
        /// <summary>
        /// Timeout for command execution in milliseconds.
        /// </summary>
        int CommandTimeoutMs { get; }
        
        /// <summary>
        /// Timeout for behavior execution in milliseconds.
        /// </summary>
        int BehaviorTimeoutMs { get; }
        
        /// <summary>
        /// Timeout for aggregator execution in milliseconds.
        /// </summary>
        int AggregatorTimeoutMs { get; }
        
        /// <summary>
        /// Maximum number of retries for failed commands.
        /// </summary>
        int MaxRetries { get; }
        
        /// <summary>
        /// Delay between retries in milliseconds.
        /// </summary>
        int RetryDelayMs { get; }
        
        /// <summary>
        /// Whether to enable detailed logging.
        /// </summary>
        bool EnableDetailedLogging { get; }
        
        /// <summary>
        /// Whether to enable performance monitoring.
        /// </summary>
        bool EnablePerformanceMonitoring { get; }
        
        /// <summary>
        /// Whether to enable execution history tracking.
        /// </summary>
        bool EnableExecutionHistory { get; }
        
        /// <summary>
        /// Maximum number of execution history entries to keep.
        /// </summary>
        int MaxExecutionHistoryEntries { get; }
        
        /// <summary>
        /// Whether to enable parallel execution by default.
        /// </summary>
        bool EnableParallelExecution { get; }
        
        /// <summary>
        /// Whether to enable dependency resolution.
        /// </summary>
        bool EnableDependencyResolution { get; }
        
        /// <summary>
        /// Whether to enable resource management.
        /// </summary>
        bool EnableResourceManagement { get; }
        
        /// <summary>
        /// Maximum memory usage in bytes.
        /// </summary>
        long MaxMemoryUsageBytes { get; }
        
        /// <summary>
        /// Maximum CPU usage percentage.
        /// </summary>
        double MaxCpuUsagePercentage { get; }
        
        /// <summary>
        /// Gets a configuration value by key.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <param name="defaultValue">Default value if key doesn't exist.</param>
        /// <returns>The configuration value.</returns>
        T GetValue<T>(string key, T defaultValue = default(T));
        
        /// <summary>
        /// Sets a configuration value.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The configuration value.</param>
        void SetValue<T>(string key, T value);
        
        /// <summary>
        /// Gets all configuration keys.
        /// </summary>
        /// <returns>All configuration keys.</returns>
        IEnumerable<string> GetKeys();
        
        /// <summary>
        /// Whether a configuration key exists.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        bool HasKey(string key);
    }
} 