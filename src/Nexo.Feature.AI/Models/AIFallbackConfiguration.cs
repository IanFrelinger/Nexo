using System.Text.Json.Serialization;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI fallback behavior when primary models are unavailable.
    /// </summary>
    public class AiFallbackConfiguration
    {
        public AiFallbackConfiguration(int maxFallbackDelaySeconds, string offlineModeResponseTemplate, bool enableLocalModelFallback, int maxCachedResponseAgeSeconds, bool enableDegradedResponseMode, double degradedResponseQualityThreshold)
        {
            MaxFallbackDelaySeconds = maxFallbackDelaySeconds;
            OfflineModeResponseTemplate = offlineModeResponseTemplate;
            EnableLocalModelFallback = enableLocalModelFallback;
            MaxCachedResponseAgeSeconds = maxCachedResponseAgeSeconds;
            EnableDegradedResponseMode = enableDegradedResponseMode;
            DegradedResponseQualityThreshold = degradedResponseQualityThreshold;
        }

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
        public int MaxFallbackDelaySeconds { get; set; }
        
        /// <summary>
        /// Whether to enable offline mode when all models are unavailable.
        /// </summary>
        [JsonPropertyName("enableOfflineMode")]
        public bool EnableOfflineMode { get; set; } = true;
        
        /// <summary>
        /// The offline mode response template.
        /// </summary>
        [JsonPropertyName("offlineModeResponseTemplate")]
        public string OfflineModeResponseTemplate { get; set; }
        
        /// <summary>
        /// Whether to enable a local model fallback.
        /// </summary>
        [JsonPropertyName("enableLocalModelFallback")]
        public bool EnableLocalModelFallback { get; set; }

        /// <summary>
        /// Whether to enable cached response fallback.
        /// </summary>
        [JsonPropertyName("enableCachedResponseFallback")]
        public bool EnableCachedResponseFallback { get; set; } = true;
        
        /// <summary>
        /// The maximum age of cached responses to use for fallback in seconds.
        /// </summary>
        [JsonPropertyName("maxCachedResponseAgeSeconds")]
        public int MaxCachedResponseAgeSeconds { get; set; } // 24 hours
        
        /// <summary>
        /// Whether to enable degraded response mode.
        /// </summary>
        [JsonPropertyName("enableDegradedResponseMode")]
        public bool EnableDegradedResponseMode { get; set; }
        
        /// <summary>
        /// The degraded response quality threshold (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("degradedResponseQualityThreshold")]
        public double DegradedResponseQualityThreshold { get; set; }
    }
} 