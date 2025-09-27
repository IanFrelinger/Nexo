using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Commands
{
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
}
