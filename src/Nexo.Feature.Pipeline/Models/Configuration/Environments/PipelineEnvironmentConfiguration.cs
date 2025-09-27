using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nexo.Feature.Pipeline.Models.Configuration.Execution;

namespace Nexo.Feature.Pipeline.Models.Configuration.Environments
{
    /// <summary>
    /// Represents a pipeline environment configuration.
    /// </summary>
    public partial class PipelineEnvironmentConfiguration
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
}
