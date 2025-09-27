using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Behaviors
{
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
}
