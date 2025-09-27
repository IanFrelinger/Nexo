using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Validation
{
    /// <summary>
    /// Represents pipeline validation configuration.
    /// </summary>
    public partial class PipelineValidationConfiguration
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
    public partial class ValidationRuleConfiguration
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
}
