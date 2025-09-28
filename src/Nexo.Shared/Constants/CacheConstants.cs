namespace Nexo.Shared
{
    /// <summary>
    /// Default cache configuration.
    /// </summary>
    public static class CacheConstants
    {
        /// <summary>
        /// Default cache expiration time in minutes.
        /// </summary>
        public const int DefaultCacheExpirationMinutes = 30;
        
        /// <summary>
        /// Default cache size limit in MB.
        /// </summary>
        public const int DefaultCacheSizeLimitMB = 100;
        
        /// <summary>
        /// Default cache cleanup interval in minutes.
        /// </summary>
        public const int DefaultCacheCleanupIntervalMinutes = 5;
        
        /// <summary>
        /// Default cache hit ratio threshold.
        /// </summary>
        public const double DefaultCacheHitRatioThreshold = 0.8;
        
        /// <summary>
        /// Default cache miss penalty in milliseconds.
        /// </summary>
        public const int DefaultCacheMissPenaltyMs = 100;
    }
}
