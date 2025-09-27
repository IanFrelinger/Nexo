using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nexo.Feature.Pipeline.Models.Configuration.Documentation
{
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
