namespace Nexo.Shared.Models
{
    /// <summary>
    /// Configuration settings for the caching system.
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// The cache backend to use (inmemory, redis).
        /// </summary>
        public string Backend { get; set; } = "inmemory";

        /// <summary>
        /// Redis connection string (required when using Redis backend).
        /// </summary>
        public string RedisConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// Redis key prefix for cache entries.
        /// </summary>
        public string RedisKeyPrefix { get; set; } = "nexo:cache:";

        /// <summary>
        /// Default TTL in seconds for cache entries.
        /// </summary>
        public int DefaultTtlSeconds { get; set; } = 300;
    }
} 