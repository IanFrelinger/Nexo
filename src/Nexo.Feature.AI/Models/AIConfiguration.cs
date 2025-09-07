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
        public AiModelConfiguration Model { get; set; } = new AiModelConfiguration(0.9, 0.0, 0.0, 3, 1);

        /// <summary>
        /// The strategy for selecting AI models.
        /// </summary>
        [JsonPropertyName("modelSelectionStrategy")]
        public ModelSelectionStrategy ModelSelectionStrategy { get; set; } = ModelSelectionStrategy.Primary;
        
        /// <summary>
        /// Configuration for AI resource allocation.
        /// </summary>
        [JsonPropertyName("resources")]
        public AiResourceConfiguration Resources { get; set; } = new AiResourceConfiguration(1000000, 1000000000, ResourceAllocationStrategy.Balanced, true, 0.5, 0.9);
        
        /// <summary>
        /// Configuration for AI performance optimization.
        /// </summary>
        [JsonPropertyName("performance")]
        public AiPerformanceConfiguration Performance { get; set; } = new AiPerformanceConfiguration(true, 10, 1000, true, 5000, true, 10, 30, true, true, 6, true, 100);
        
        /// <summary>
        /// Configuration for AI response caching.
        /// </summary>
        [JsonPropertyName("caching")]
        public AiCachingConfiguration Caching { get; set; } = new AiCachingConfiguration(true, 60, false, 300, false, "cache.json", 300, 3600);
        
        /// <summary>
        /// Configuration for AI fallback behavior.
        /// </summary>
        [JsonPropertyName("fallback")]
        public AiFallbackConfiguration Fallback { get; set; } = new AiFallbackConfiguration(30, "Service temporarily unavailable", true, 300, true, 0.7);
        
        /// <summary>
        /// Configuration for AI monitoring and observability.
        /// </summary>
        [JsonPropertyName("monitoring")]
        public AiMonitoringConfiguration Monitoring { get; set; } = new AiMonitoringConfiguration(true, 30, true, "https://telemetry.example.com", true, 0.1, true);
    }
} 