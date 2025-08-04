using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI model settings and parameters.
    /// </summary>
    public class AIModelConfiguration
    {
        /// <summary>
        /// The name or identifier of the AI model.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "gpt-4";
        
        /// <summary>
        /// The version of the AI model.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "latest";
        
        /// <summary>
        /// The maximum number of tokens for input context.
        /// </summary>
        [JsonPropertyName("maxInputTokens")]
        public int MaxInputTokens { get; set; } = 8192;
        
        /// <summary>
        /// The maximum number of tokens for output response.
        /// </summary>
        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 4096;
        
        /// <summary>
        /// The temperature setting for response randomness (0.0 to 2.0).
        /// </summary>
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
        
        /// <summary>
        /// The top-p setting for nucleus sampling (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("topP")]
        public double TopP { get; set; } = 0.9;
        
        /// <summary>
        /// The frequency penalty for reducing repetition (-2.0 to 2.0).
        /// </summary>
        [JsonPropertyName("frequencyPenalty")]
        public double FrequencyPenalty { get; set; } = 0.0;
        
        /// <summary>
        /// The presence penalty for encouraging new topics (-2.0 to 2.0).
        /// </summary>
        [JsonPropertyName("presencePenalty")]
        public double PresencePenalty { get; set; } = 0.0;
        
        /// <summary>
        /// Whether to enable streaming responses.
        /// </summary>
        [JsonPropertyName("enableStreaming")]
        public bool EnableStreaming { get; set; } = true;
        
        /// <summary>
        /// The timeout for model requests in seconds.
        /// </summary>
        [JsonPropertyName("requestTimeoutSeconds")]
        public int RequestTimeoutSeconds { get; set; } = 60;
        
        /// <summary>
        /// The number of retry attempts for failed requests.
        /// </summary>
        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// The delay between retry attempts in seconds.
        /// </summary>
        [JsonPropertyName("retryDelaySeconds")]
        public int RetryDelaySeconds { get; set; } = 2;
        
        /// <summary>
        /// Custom parameters specific to the model provider.
        /// </summary>
        [JsonPropertyName("customParameters")]
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }
} 