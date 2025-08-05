using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI performance optimization settings.
    /// </summary>
    public class AiPerformanceConfiguration
    {
        public AiPerformanceConfiguration(bool enableRequestBatching, int maxBatchSize, int batchTimeoutMs, bool enableRequestDeduplication, int deduplicationWindowMs, bool enableConnectionPooling, int maxConnectionPoolSize, int connectionPoolTimeoutSeconds, bool enableRequestCompression, bool enableResponseCompression, int compressionLevel, bool enableRequestPrioritization, int priorityQueueSize)
        {
            EnableRequestBatching = enableRequestBatching;
            MaxBatchSize = maxBatchSize;
            BatchTimeoutMs = batchTimeoutMs;
            EnableRequestDeduplication = enableRequestDeduplication;
            DeduplicationWindowMs = deduplicationWindowMs;
            EnableConnectionPooling = enableConnectionPooling;
            MaxConnectionPoolSize = maxConnectionPoolSize;
            ConnectionPoolTimeoutSeconds = connectionPoolTimeoutSeconds;
            EnableRequestCompression = enableRequestCompression;
            EnableResponseCompression = enableResponseCompression;
            CompressionLevel = compressionLevel;
            EnableRequestPrioritization = enableRequestPrioritization;
            PriorityQueueSize = priorityQueueSize;
        }

        /// <summary>
        /// The performance mode for AI operations.
        /// </summary>
        [JsonPropertyName("mode")]
        public PerformanceMode Mode { get; set; } = PerformanceMode.Balanced;
        
        /// <summary>
        /// The target response time in milliseconds.
        /// </summary>
        [JsonPropertyName("targetResponseTimeMs")]
        public int TargetResponseTimeMs { get; set; } = 5000;
        
        /// <summary>
        /// The maximum response time in milliseconds.
        /// </summary>
        [JsonPropertyName("maxResponseTimeMs")]
        public int MaxResponseTimeMs { get; set; } = 30000;
        
        /// <summary>
        /// Whether to enable request batching.
        /// </summary>
        [JsonPropertyName("enableRequestBatching")]
        public bool EnableRequestBatching { get; set; }
        
        /// <summary>
        /// The maximum batch size for requests.
        /// </summary>
        [JsonPropertyName("maxBatchSize")]
        public int MaxBatchSize { get; set; }
        
        /// <summary>
        /// The batch timeout in milliseconds.
        /// </summary>
        [JsonPropertyName("batchTimeoutMs")]
        public int BatchTimeoutMs { get; set; }
        
        /// <summary>
        /// Whether to enable response caching.
        /// </summary>
        [JsonPropertyName("enableResponseCaching")]
        public bool EnableResponseCaching { get; set; } = true;
        
        /// <summary>
        /// The cache expiration time in seconds.
        /// </summary>
        [JsonPropertyName("cacheExpirationSeconds")]
        public int CacheExpirationSeconds { get; set; } = 3600;
        
        /// <summary>
        /// Whether to enable request deduplication.
        /// </summary>
        [JsonPropertyName("enableRequestDeduplication")]
        public bool EnableRequestDeduplication { get; set; }
        
        /// <summary>
        /// The deduplication window in milliseconds.
        /// </summary>
        [JsonPropertyName("deduplicationWindowMs")]
        public int DeduplicationWindowMs { get; set; }
        
        /// <summary>
        /// Whether to enable connection pooling.
        /// </summary>
        [JsonPropertyName("enableConnectionPooling")]
        public bool EnableConnectionPooling { get; set; }
        
        /// <summary>
        /// The maximum number of connections in the pool.
        /// </summary>
        [JsonPropertyName("maxConnectionPoolSize")]
        public int MaxConnectionPoolSize { get; set; }
        
        /// <summary>
        /// The connection pool timeout in seconds.
        /// </summary>
        [JsonPropertyName("connectionPoolTimeoutSeconds")]
        public int ConnectionPoolTimeoutSeconds { get; set; }
        
        /// <summary>
        /// Whether to enable request compression.
        /// </summary>
        [JsonPropertyName("enableRequestCompression")]
        public bool EnableRequestCompression { get; set; }
        
        /// <summary>
        /// Whether to enable response compression.
        /// </summary>
        [JsonPropertyName("enableResponseCompression")]
        public bool EnableResponseCompression { get; set; }
        
        /// <summary>
        /// The compression level (0-9).
        /// </summary>
        [JsonPropertyName("compressionLevel")]
        public int CompressionLevel { get; set; }
        
        /// <summary>
        /// Whether to enable request prioritization.
        /// </summary>
        [JsonPropertyName("enableRequestPrioritization")]
        public bool EnableRequestPrioritization { get; set; }
        
        /// <summary>
        /// The priority queue size.
        /// </summary>
        [JsonPropertyName("priorityQueueSize")]
        public int PriorityQueueSize { get; set; }
     }
} 