using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI fallback behavior when primary models are unavailable.
    /// </summary>
    public class AIFallbackConfiguration
    {
        /// <summary>
        /// Whether to enable fallback behavior.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// The maximum number of fallback attempts.
        /// </summary>
        [JsonPropertyName("maxFallbackAttempts")]
        public int MaxFallbackAttempts { get; set; } = 3;
        
        /// <summary>
        /// The delay between fallback attempts in seconds.
        /// </summary>
        [JsonPropertyName("fallbackDelaySeconds")]
        public int FallbackDelaySeconds { get; set; } = 5;
        
        /// <summary>
        /// Whether to enable exponential backoff for fallback attempts.
        /// </summary>
        [JsonPropertyName("enableExponentialBackoff")]
        public bool EnableExponentialBackoff { get; set; } = true;
        
        /// <summary>
        /// The maximum fallback delay in seconds.
        /// </summary>
        [JsonPropertyName("maxFallbackDelaySeconds")]
        public int MaxFallbackDelaySeconds { get; set; } = 60;
        
        /// <summary>
        /// Whether to enable offline mode when all models are unavailable.
        /// </summary>
        [JsonPropertyName("enableOfflineMode")]
        public bool EnableOfflineMode { get; set; } = true;
        
        /// <summary>
        /// The offline mode response template.
        /// </summary>
        [JsonPropertyName("offlineModeResponseTemplate")]
        public string OfflineModeResponseTemplate { get; set; } = "I'm currently offline and cannot process your request. Please try again later.";
        
        /// <summary>
        /// Whether to enable local model fallback.
        /// </summary>
        [JsonPropertyName("enableLocalModelFallback")]
        public bool EnableLocalModelFallback { get; set; } = false;
        
        /// <summary>
        /// The local model configuration for fallback.
        /// </summary>
        [JsonPropertyName("localModelConfiguration")]
        public AIModelConfiguration LocalModelConfiguration { get; set; } = new AIModelConfiguration();
        
        /// <summary>
        /// Whether to enable cached response fallback.
        /// </summary>
        [JsonPropertyName("enableCachedResponseFallback")]
        public bool EnableCachedResponseFallback { get; set; } = true;
        
        /// <summary>
        /// The maximum age of cached responses to use for fallback in seconds.
        /// </summary>
        [JsonPropertyName("maxCachedResponseAgeSeconds")]
        public int MaxCachedResponseAgeSeconds { get; set; } = 86400; // 24 hours
        
        /// <summary>
        /// Whether to enable degraded response mode.
        /// </summary>
        [JsonPropertyName("enableDegradedResponseMode")]
        public bool EnableDegradedResponseMode { get; set; } = true;
        
        /// <summary>
        /// The degraded response quality threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("degradedResponseQualityThreshold")]
        public double DegradedResponseQualityThreshold { get; set; } = 0.5;
        
        /// <summary>
        /// Custom fallback settings.
        /// </summary>
        [JsonPropertyName("customSettings")]
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }
} 