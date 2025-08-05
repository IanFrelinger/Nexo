using System.Text.Json.Serialization;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models
{
    /// <summary>
    /// Configuration for AI response caching settings.
    /// </summary>
    public class AiCachingConfiguration
    {
        public AiCachingConfiguration(bool enableStatistics, int statisticsIntervalSeconds, bool enableCacheWarming, int cacheWarmingIntervalSeconds, bool enablePersistence, string persistenceFilePath, int persistenceIntervalSeconds, int maxExpirationSeconds)
        {
            EnableStatistics = enableStatistics;
            StatisticsIntervalSeconds = statisticsIntervalSeconds;
            EnableCacheWarming = enableCacheWarming;
            CacheWarmingIntervalSeconds = cacheWarmingIntervalSeconds;
            EnablePersistence = enablePersistence;
            PersistenceFilePath = persistenceFilePath;
            PersistenceIntervalSeconds = persistenceIntervalSeconds;
            MaxExpirationSeconds = maxExpirationSeconds;
        }

        /// <summary>
        /// Whether to enable response caching.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// The maximum cache size in bytes.
        /// </summary>
        [JsonPropertyName("maxCacheSizeBytes")]
        public long MaxCacheSizeBytes { get; set; } = 1L * 1024L * 1024L * 1024L; // 1GB
        
        /// <summary>
        /// The default cache expiration time in seconds.
        /// </summary>
        [JsonPropertyName("defaultExpirationSeconds")]
        public int DefaultExpirationSeconds { get; set; } = 3600; // 1 hour
        
        /// <summary>
        /// The maximum cache expiration time in seconds.
        /// </summary>
        [JsonPropertyName("maxExpirationSeconds")]
        public int MaxExpirationSeconds { get; set; } // 24 hours
        
        /// <summary>
        /// The cache eviction policy.
        /// </summary>
        [JsonPropertyName("evictionPolicy")]
        public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LeastRecentlyUsed;
        
        /// <summary>
        /// Whether to enable cache compression.
        /// </summary>
        [JsonPropertyName("enableCompression")]
        public bool EnableCompression { get; set; } = true;
        
        /// <summary>
        /// The compression level (0-9).
        /// </summary>
        [JsonPropertyName("compressionLevel")]
        public int CompressionLevel { get; set; } = 6;
        
        /// <summary>
        /// Whether to enable cache statistics.
        /// </summary>
        [JsonPropertyName("enableStatistics")]
        public bool EnableStatistics { get; set; }
        
        /// <summary>
        /// The cache statistics collection interval in seconds.
        /// </summary>
        [JsonPropertyName("statisticsIntervalSeconds")]
        public int StatisticsIntervalSeconds { get; set; } // 5 minutes
        
        /// <summary>
        /// Whether to enable cache warming.
        /// </summary>
        [JsonPropertyName("enableCacheWarming")]
        public bool EnableCacheWarming { get; set; }
        
        /// <summary>
        /// The cache warming interval in seconds.
        /// </summary>
        [JsonPropertyName("cacheWarmingIntervalSeconds")]
        public int CacheWarmingIntervalSeconds { get; set; } // 1 hour
        
        /// <summary>
        /// Whether to enable cache persistence.
        /// </summary>
        [JsonPropertyName("enablePersistence")]
        public bool EnablePersistence { get; set; }
        
        /// <summary>
        /// The cache persistence file path.
        /// </summary>
        [JsonPropertyName("persistenceFilePath")]
        public string PersistenceFilePath { get; set; }
        
        /// <summary>
        /// The cache persistence interval in seconds.
        /// </summary>
        [JsonPropertyName("persistenceIntervalSeconds")]
        public int PersistenceIntervalSeconds { get; set; } // 5 minutes
    }
} 