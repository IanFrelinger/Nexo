using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Aggregators
{
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
}
