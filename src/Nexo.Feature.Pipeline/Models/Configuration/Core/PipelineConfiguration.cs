using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Models.Configuration.Core
{
    /// <summary>
    /// Represents a complete pipeline configuration that can be loaded from files or command line.
    /// </summary>
    public partial class PipelineConfiguration : IPipelineConfiguration
    {
        /// <summary>
        /// Gets or sets the pipeline name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline version.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the pipeline description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline author.
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline tags for categorization.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the pipeline execution settings.
        /// </summary>
        [JsonPropertyName("execution")]
        public PipelineExecutionSettings? Execution { get; set; } = new PipelineExecutionSettings();

        /// <summary>
        /// Gets or sets the pipeline commands configuration.
        /// </summary>
        [JsonPropertyName("commands")]
        public List<PipelineCommandConfiguration> Commands { get; set; } = new List<PipelineCommandConfiguration>();

        /// <summary>
        /// Gets or sets the pipeline behaviors configuration.
        /// </summary>
        [JsonPropertyName("behaviors")]
        public List<PipelineBehaviorConfiguration> Behaviors { get; set; } = new List<PipelineBehaviorConfiguration>();

        /// <summary>
        /// Gets or sets the pipeline aggregators configuration.
        /// </summary>
        [JsonPropertyName("aggregators")]
        public List<PipelineAggregatorConfiguration> Aggregators { get; set; } = new List<PipelineAggregatorConfiguration>();

        /// <summary>
        /// Gets or sets the pipeline variables and parameters.
        /// </summary>
        [JsonPropertyName("variables")]
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the pipeline environment-specific configurations.
        /// </summary>
        [JsonPropertyName("environments")]
        public Dictionary<string, PipelineEnvironmentConfiguration> Environments { get; set; } = new Dictionary<string, PipelineEnvironmentConfiguration>();

        /// <summary>
        /// Gets or sets the pipeline validation rules.
        /// </summary>
        [JsonPropertyName("validation")]
        public PipelineValidationConfiguration Validation { get; set; } = new PipelineValidationConfiguration();

        /// <summary>
        /// Gets or sets the pipeline documentation.
        /// </summary>
        [JsonPropertyName("documentation")]
        public PipelineDocumentationConfiguration Documentation { get; set; } = new PipelineDocumentationConfiguration();

        /// <summary>
        /// Gets or sets the pipeline identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        // IPipelineConfiguration implementation
        public int MaxParallelExecutions => Execution?.MaxParallelExecutions ?? 1;
        public int CommandTimeoutMs => Execution?.CommandTimeoutMs ?? 30000;
        public int BehaviorTimeoutMs => Execution?.BehaviorTimeoutMs ?? 60000;
        public int AggregatorTimeoutMs => Execution?.AggregatorTimeoutMs ?? 120000;
        public int MaxRetries => Execution?.MaxRetries ?? 3;
        public int RetryDelayMs => Execution?.RetryDelayMs ?? 1000;
        public bool EnableDetailedLogging => Execution?.EnableDetailedLogging ?? false;
        public bool EnablePerformanceMonitoring => Execution?.EnablePerformanceMonitoring ?? false;
        public bool EnableExecutionHistory => Execution?.EnableExecutionHistory ?? false;
        public int MaxExecutionHistoryEntries => Execution?.MaxExecutionHistoryEntries ?? 100;
        public bool EnableParallelExecution => Execution?.EnableParallelExecution ?? true;
        public bool EnableDependencyResolution => Execution?.EnableDependencyResolution ?? true;
        public bool EnableResourceManagement => Execution?.EnableResourceManagement ?? false;
        public long MaxMemoryUsageBytes => Execution?.MaxMemoryUsageBytes ?? 1073741824; // 1GB
        public double MaxCpuUsagePercentage => Execution?.MaxCpuUsagePercentage ?? 80.0;

        public T? GetValue<T>(string key, T? defaultValue = default(T))
        {
            if (Variables.TryGetValue(key, out var value) && value is T tValue)
                return tValue;
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            Variables[key] = value!;
        }

        public IEnumerable<string> GetKeys()
        {
            return Variables.Keys;
        }

        public bool HasKey(string key)
        {
            return Variables.ContainsKey(key);
        }
    }
}
