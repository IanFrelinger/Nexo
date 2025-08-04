using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Represents information about an AI model.
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the model.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the model.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the model.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version of the model.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the model.
        /// </summary>
        [JsonPropertyName("type")]
        public ModelType Type { get; set; }

        /// <summary>
        /// Gets or sets the provider of the model.
        /// </summary>
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the model in bytes.
        /// </summary>
        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of parameters in the model.
        /// </summary>
        [JsonPropertyName("parameterCount")]
        public long ParameterCount { get; set; }

        /// <summary>
        /// Gets or sets the context window size.
        /// </summary>
        [JsonPropertyName("contextWindowSize")]
        public int ContextWindowSize { get; set; }

        /// <summary>
        /// Gets or sets the capabilities of the model.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public ModelCapabilities Capabilities { get; set; } = new ModelCapabilities();

        /// <summary>
        /// Gets or sets the tags associated with the model.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether the model is available for use.
        /// </summary>
        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata for the model.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }



    /// <summary>
    /// Represents a response from an AI model.
    /// </summary>
    public class ModelResponse
    {
        /// <summary>
        /// Gets or sets the generated text or content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model that generated the response.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of tokens used in the input.
        /// </summary>
        [JsonPropertyName("inputTokens")]
        public int InputTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens generated in the output.
        /// </summary>
        [JsonPropertyName("outputTokens")]
        public int OutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of tokens used.
        /// </summary>
        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used (alias for TotalTokens).
        /// </summary>
        public int TokensUsed => TotalTokens;

        /// <summary>
        /// Gets or sets the cost of the request.
        /// </summary>
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        [JsonPropertyName("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the response.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a chunk of a streaming model response.
    /// </summary>
    public class ModelResponseChunk
    {
        /// <summary>
        /// Gets or sets the content of the chunk.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this is the final chunk.
        /// </summary>
        [JsonPropertyName("isFinal")]
        public bool IsFinal { get; set; } = false;

        /// <summary>
        /// Gets or sets the index of the chunk.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the chunk.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the validation result for a model.
    /// </summary>
    public class ModelValidationResult
    {
        /// <summary>
        /// Gets or sets whether the model is valid.
        /// </summary>
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation information messages.
        /// </summary>
        [JsonPropertyName("information")]
        public List<string> Information { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents the health status of a model provider.
    /// </summary>
    public class ProviderHealthStatus
    {
        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        [JsonPropertyName("providerId")]
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the provider is healthy.
        /// </summary>
        [JsonPropertyName("isHealthy")]
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [JsonPropertyName("statusMessage")]
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        [JsonPropertyName("responseTimeMs")]
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the last health check timestamp.
        /// </summary>
        [JsonPropertyName("lastHealthCheck")]
        public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the number of available models.
        /// </summary>
        [JsonPropertyName("availableModelsCount")]
        public int AvailableModelsCount { get; set; }

        /// <summary>
        /// Gets or sets the error count.
        /// </summary>
        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the provider.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 