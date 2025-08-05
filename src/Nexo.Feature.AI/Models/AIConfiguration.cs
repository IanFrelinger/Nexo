using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Comprehensive configuration for AI services and operations.
    /// </summary>
    public class AiConfiguration
    {
        /// <summary>
        /// The AI operation mode.
        /// </summary>
        [JsonPropertyName("mode")]
        public AiMode Mode { get; set; } = AiMode.Production;
        
        /// <summary>
        /// Configuration for AI model settings.
        /// </summary>
        [JsonPropertyName("model")]
        public AiModelConfiguration Model { get; set; } = new AiModelConfiguration();

        /// <summary>
        /// The strategy for selecting AI models.
        /// </summary>
        [JsonPropertyName("modelSelectionStrategy")]
        public ModelSelectionStrategy ModelSelectionStrategy { get; set; } = ModelSelectionStrategy.Primary;
        
        /// <summary>
        /// Configuration for AI resource allocation.
        /// </summary>
        [JsonPropertyName("resources")]
        public AiResourceConfiguration Resources { get; set; } = new();
        
        /// <summary>
        /// Configuration for AI performance optimization.
        /// </summary>
        [JsonPropertyName("performance")]
        public AiPerformanceConfiguration Performance { get; set; } = new();
        
        /// <summary>
        /// Configuration for AI response caching.
        /// </summary>
        [JsonPropertyName("caching")]
        public AiCachingConfiguration Caching { get; set; } = new AiCachingConfiguration();
        
        /// <summary>
        /// Configuration for AI fallback behavior.
        /// </summary>
        [JsonPropertyName("fallback")]
        public AiFallbackConfiguration Fallback { get; set; } = new AiFallbackConfiguration();
        
        /// <summary>
        /// Configuration for AI monitoring and observability.
        /// </summary>
        [JsonPropertyName("monitoring")]
        public AiMonitoringConfiguration Monitoring { get; set; } = new();
    }
} 