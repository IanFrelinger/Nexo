using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Represents a request to an AI model.
    /// </summary>
    public class ModelRequest
    {
        public ModelRequest(double topP, double frequencyPenalty, double presencePenalty, bool stream)
        {
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
            Stream = stream;
        }

        /// <summary>
        /// Gets or sets the input prompt or content.
        /// </summary>
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system prompt or instructions.
        /// </summary>
        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate.
        /// </summary>
        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the temperature for generation (0.0 to 2.0).
        /// </summary>
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the top-p value for nucleus sampling.
        /// </summary>
        [JsonPropertyName("topP")]
        public double TopP { get; set; }

        /// <summary>
        /// Gets or sets the frequency penalty.
        /// </summary>
        [JsonPropertyName("frequencyPenalty")]
        public double FrequencyPenalty { get; set; }

        /// <summary>
        /// Gets or sets the presence penalty.
        /// </summary>
        [JsonPropertyName("presencePenalty")]
        public double PresencePenalty { get; set; }

        /// <summary>
        /// Gets or sets whether to stream the response.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the request.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
} 