using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a complete pipeline configuration that can be loaded from files or command line.
    /// </summary>
    public class PipelineConfiguration : IPipelineConfiguration
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
            Variables[key] = value;
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

    /// <summary>
    /// Represents a pipeline command configuration.
    /// </summary>
    public class PipelineCommandConfiguration
    {
        /// <summary>
        /// Gets or sets the command ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command category.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command priority.
        /// </summary>
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Gets or sets the command parameters.
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the command dependencies.
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether the command can execute in parallel.
        /// </summary>
        [JsonPropertyName("canExecuteInParallel")]
        public bool CanExecuteInParallel { get; set; } = true;

        /// <summary>
        /// Gets or sets the command timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the command retry settings.
        /// </summary>
        [JsonPropertyName("retry")]
        public CommandRetryConfiguration? Retry { get; set; }

        /// <summary>
        /// Gets or sets the command validation rules.
        /// </summary>
        [JsonPropertyName("validation")]
        public List<string> Validation { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents command retry configuration.
    /// </summary>
    public class CommandRetryConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum retries.
        /// </summary>
        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry delay in milliseconds.
        /// </summary>
        [JsonPropertyName("delayMs")]
        public int DelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the retry backoff multiplier.
        /// </summary>
        [JsonPropertyName("backoffMultiplier")]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the maximum retry delay in milliseconds.
        /// </summary>
        [JsonPropertyName("maxDelayMs")]
        public int MaxDelayMs { get; set; } = 30000;
    }

    /// <summary>
    /// Represents a pipeline behavior configuration.
    /// </summary>
    public class PipelineBehaviorConfiguration
    {
        /// <summary>
        /// Gets or sets the behavior ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the behavior name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the behavior description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the behavior execution strategy.
        /// </summary>
        [JsonPropertyName("executionStrategy")]
        public string ExecutionStrategy { get; set; } = "Sequential";

        /// <summary>
        /// Gets or sets the behavior commands.
        /// </summary>
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the behavior dependencies.
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the behavior conditions.
        /// </summary>
        [JsonPropertyName("conditions")]
        public List<string> Conditions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a pipeline aggregator configuration.
    /// </summary>
    public class PipelineAggregatorConfiguration
    {
        /// <summary>
        /// Gets or sets the aggregator ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregator name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregator description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregator execution strategy.
        /// </summary>
        [JsonPropertyName("executionStrategy")]
        public string ExecutionStrategy { get; set; } = "Sequential";

        /// <summary>
        /// Gets or sets the aggregator behaviors.
        /// </summary>
        [JsonPropertyName("behaviors")]
        public List<string> Behaviors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the aggregator dependencies.
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the aggregator resource requirements.
        /// </summary>
        [JsonPropertyName("resourceRequirements")]
        public ResourceRequirementsConfiguration? ResourceRequirements { get; set; }
    }

    /// <summary>
    /// Represents resource requirements configuration.
    /// </summary>
    public class ResourceRequirementsConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum memory in bytes.
        /// </summary>
        [JsonPropertyName("minMemoryBytes")]
        public long MinMemoryBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum memory in bytes.
        /// </summary>
        [JsonPropertyName("maxMemoryBytes")]
        public long MaxMemoryBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum CPU cores.
        /// </summary>
        [JsonPropertyName("minCpuCores")]
        public int MinCpuCores { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum CPU cores.
        /// </summary>
        [JsonPropertyName("maxCpuCores")]
        public int MaxCpuCores { get; set; } = 0;

        /// <summary>
        /// Gets or sets the required disk space in bytes.
        /// </summary>
        [JsonPropertyName("requiredDiskSpaceBytes")]
        public long RequiredDiskSpaceBytes { get; set; } = 0;
    }

    /// <summary>
    /// Represents a pipeline environment configuration.
    /// </summary>
    public class PipelineEnvironmentConfiguration
    {
        /// <summary>
        /// Gets or sets the environment variables.
        /// </summary>
        [JsonPropertyName("variables")]
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the environment-specific execution settings.
        /// </summary>
        [JsonPropertyName("execution")]
        public PipelineExecutionSettings Execution { get; set; } = new PipelineExecutionSettings();

        /// <summary>
        /// Gets or sets the environment-specific command overrides.
        /// </summary>
        [JsonPropertyName("commandOverrides")]
        public Dictionary<string, object> CommandOverrides { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents pipeline validation configuration.
    /// </summary>
    public class PipelineValidationConfiguration
    {
        /// <summary>
        /// Gets or sets the validation rules.
        /// </summary>
        [JsonPropertyName("rules")]
        public List<ValidationRuleConfiguration> Rules { get; set; } = new List<ValidationRuleConfiguration>();

        /// <summary>
        /// Gets or sets whether to fail on validation errors.
        /// </summary>
        [JsonPropertyName("failOnError")]
        public bool FailOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets the validation timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Represents a validation rule configuration.
    /// </summary>
    public class ValidationRuleConfiguration
    {
        /// <summary>
        /// Gets or sets the rule name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rule description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rule type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rule parameters.
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the rule severity.
        /// </summary>
        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "Error";
    }

    /// <summary>
    /// Represents pipeline documentation configuration.
    /// </summary>
    public class PipelineDocumentationConfiguration
    {
        /// <summary>
        /// Gets or sets the documentation summary.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the documentation details.
        /// </summary>
        [JsonPropertyName("details")]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the documentation examples.
        /// </summary>
        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the documentation tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the documentation links.
        /// </summary>
        [JsonPropertyName("links")]
        public List<string> Links { get; set; } = new List<string>();
    }
} 