using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Represents the capabilities of an AI model.
    /// </summary>
    public class ModelCapabilities
    {
        public ModelCapabilities(bool supportsTextGeneration, bool supportsCodeGeneration, bool supportsCodeAnalysis, bool supportsImageGeneration, bool supportsImageAnalysis)
        {
            SupportsTextGeneration = supportsTextGeneration;
            SupportsCodeGeneration = supportsCodeGeneration;
            SupportsCodeAnalysis = supportsCodeAnalysis;
            SupportsImageGeneration = supportsImageGeneration;
            SupportsImageAnalysis = supportsImageAnalysis;
        }

        /// <summary>
        /// Gets or sets whether the model supports text generation.
        /// </summary>
        [JsonPropertyName("supportsTextGeneration")]
        public bool SupportsTextGeneration { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports code generation.
        /// </summary>
        [JsonPropertyName("supportsCodeGeneration")]
        public bool SupportsCodeGeneration { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports code analysis.
        /// </summary>
        [JsonPropertyName("supportsCodeAnalysis")]
        public bool SupportsCodeAnalysis { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports text embedding.
        /// </summary>
        [JsonPropertyName("supportsTextEmbedding")]
        public bool SupportsTextEmbedding { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports image generation.
        /// </summary>
        [JsonPropertyName("supportsImageGeneration")]
        public bool SupportsImageGeneration { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports image analysis.
        /// </summary>
        [JsonPropertyName("supportsImageAnalysis")]
        public bool SupportsImageAnalysis { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports streaming responses.
        /// </summary>
        [JsonPropertyName("supportsStreaming")]
        public bool SupportsStreaming { get; set; }

        /// <summary>
        /// Gets or sets whether the model supports function calling.
        /// </summary>
        [JsonPropertyName("supportsFunctionCalling")]
        public bool SupportsFunctionCalling { get; set; }

        /// <summary>
        /// Gets or sets the maximum input length.
        /// </summary>
        [JsonPropertyName("maxInputLength")]
        public int MaxInputLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum output length.
        /// </summary>
        [JsonPropertyName("maxOutputLength")]
        public int MaxOutputLength { get; set; }

        /// <summary>
        /// Gets or sets the supported languages.
        /// </summary>
        [JsonPropertyName("supportedLanguages")]
        public List<string> SupportedLanguages { get; set; } = [];
    }
} 