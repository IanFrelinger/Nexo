using System;

namespace Nexo.Shared
{
    /// <summary>
    /// Configuration settings for cache backend selection and Redis connection.
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// The cache backend to use: "InMemory" or "Redis".
        /// </summary>
        public string Backend { get; set; } = "InMemory";

        /// <summary>
        /// The Redis connection string (used if Backend is "Redis").
        /// </summary>
        public string RedisConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// The Redis key prefix (optional).
        /// </summary>
        public string RedisKeyPrefix { get; set; } = "nexo:cache:";

        /// <summary>
        /// Default cache TTL in seconds (optional).
        /// </summary>
        public int? DefaultTtlSeconds { get; set; }
    }
} 