using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Comprehensive configuration for AI services and operations.
    /// </summary>
    public class AIConfiguration
    {
        /// <summary>
        /// The AI operation mode.
        /// </summary>
        [JsonPropertyName("mode")]
        public AIMode Mode { get; set; } = AIMode.Production;
        
        /// <summary>
        /// Configuration for AI model settings.
        /// </summary>
        [JsonPropertyName("model")]
        public AIModelConfiguration Model { get; set; } = new AIModelConfiguration();
        
        /// <summary>
        /// Configuration for AI model providers.
        /// </summary>
        [JsonPropertyName("providers")]
        public List<AIModelProviderConfiguration> Providers { get; set; } = new List<AIModelProviderConfiguration>();
        
        /// <summary>
        /// The strategy for selecting AI models.
        /// </summary>
        [JsonPropertyName("modelSelectionStrategy")]
        public ModelSelectionStrategy ModelSelectionStrategy { get; set; } = ModelSelectionStrategy.Primary;
        
        /// <summary>
        /// Configuration for AI resource allocation.
        /// </summary>
        [JsonPropertyName("resources")]
        public AIResourceConfiguration Resources { get; set; } = new AIResourceConfiguration();
        
        /// <summary>
        /// Configuration for AI performance optimization.
        /// </summary>
        [JsonPropertyName("performance")]
        public AIPerformanceConfiguration Performance { get; set; } = new AIPerformanceConfiguration();
        
        /// <summary>
        /// Configuration for AI response caching.
        /// </summary>
        [JsonPropertyName("caching")]
        public AICachingConfiguration Caching { get; set; } = new AICachingConfiguration();
        
        /// <summary>
        /// Configuration for AI fallback behavior.
        /// </summary>
        [JsonPropertyName("fallback")]
        public AIFallbackConfiguration Fallback { get; set; } = new AIFallbackConfiguration();
        
        /// <summary>
        /// Configuration for AI monitoring and observability.
        /// </summary>
        [JsonPropertyName("monitoring")]
        public AIMonitoringConfiguration Monitoring { get; set; } = new AIMonitoringConfiguration();
        
        /// <summary>
        /// Custom configuration settings.
        /// </summary>
        [JsonPropertyName("customSettings")]
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }
} 