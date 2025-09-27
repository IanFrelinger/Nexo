using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Shared;

namespace Nexo.CLI.Program.Configuration
{
    /// <summary>
    /// Stub implementation of IPipelineConfiguration for CLI usage
    /// </summary>
    public partial class StubPipelineConfiguration : IPipelineConfiguration
    {
        public int MaxParallelExecutions => Constants.Limits.DefaultMaxParallelExecutions;
        public int CommandTimeoutMs => Constants.Timeouts.DefaultCommandTimeoutMs;
        public int BehaviorTimeoutMs => Constants.Timeouts.DefaultBehaviorTimeoutMs;
        public int AggregatorTimeoutMs => Constants.Timeouts.DefaultAggregatorTimeoutMs;
        public int MaxRetries => Constants.Retry.DefaultMaxRetries;
        public int RetryDelayMs => Constants.Timeouts.DefaultRetryDelayMs;
        public bool EnableDetailedLogging => false;
        public bool EnablePerformanceMonitoring => false;
        public bool EnableExecutionHistory => false;
        public int MaxExecutionHistoryEntries => Constants.Limits.DefaultMaxExecutionHistoryEntries;
        public bool EnableParallelExecution => false;
        public bool EnableDependencyResolution => false;
        public bool EnableResourceManagement => false;
        public long MaxMemoryUsageBytes => Constants.Limits.DefaultMaxMemoryUsageBytes;
        public double MaxCpuUsagePercentage => Constants.Limits.DefaultMaxCpuUsagePercentage;
        public T GetValue<T>(string key, T defaultValue = default(T)!) => defaultValue;
        public void SetValue<T>(string key, T value) { }
        public IEnumerable<string> GetKeys() => Array.Empty<string>();
        public bool HasKey(string key) => false;
    }
}
