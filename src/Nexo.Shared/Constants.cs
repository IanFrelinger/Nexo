namespace Nexo.Shared
{
    /// <summary>
    /// Application-wide constants to replace magic numbers and strings.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Default timeout values in milliseconds.
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Default command timeout in milliseconds.
            /// </summary>
            public const int DefaultCommandTimeoutMs = 10000;
            
            /// <summary>
            /// Default behavior timeout in milliseconds.
            /// </summary>
            public const int DefaultBehaviorTimeoutMs = 30000;
            
            /// <summary>
            /// Default aggregator timeout in milliseconds.
            /// </summary>
            public const int DefaultAggregatorTimeoutMs = 60000;
            
            /// <summary>
            /// Default retry delay in milliseconds.
            /// </summary>
            public const int DefaultRetryDelayMs = 1000;
            
            /// <summary>
            /// Default HTTP request timeout in seconds.
            /// </summary>
            public const int DefaultHttpTimeoutSeconds = 30;
        }

        /// <summary>
        /// Default retry configuration.
        /// </summary>
        public static class Retry
        {
            /// <summary>
            /// Default maximum number of retries.
            /// </summary>
            public const int DefaultMaxRetries = 3;
            
            /// <summary>
            /// Default maximum retries for critical operations.
            /// </summary>
            public const int CriticalMaxRetries = 5;
        }

        /// <summary>
        /// Resource limits and thresholds.
        /// </summary>
        public static class Limits
        {
            /// <summary>
            /// Default maximum memory usage in bytes (1GB).
            /// </summary>
            public const long DefaultMaxMemoryUsageBytes = 1024 * 1024 * 1024;
            
            /// <summary>
            /// Default maximum CPU usage percentage.
            /// </summary>
            public const double DefaultMaxCpuUsagePercentage = 80.0;
            
            /// <summary>
            /// Default maximum parallel executions.
            /// </summary>
            public const int DefaultMaxParallelExecutions = 4;
            
            /// <summary>
            /// Default maximum execution history entries.
            /// </summary>
            public const int DefaultMaxExecutionHistoryEntries = 1000;
        }

        /// <summary>
        /// Cache configuration defaults.
        /// </summary>
        public static class Cache
        {
            /// <summary>
            /// Default cache TTL in seconds (5 minutes).
            /// </summary>
            public const int DefaultTtlSeconds = 300;
            
            /// <summary>
            /// Long-term cache TTL in seconds (1 hour).
            /// </summary>
            public const int LongTermTtlSeconds = 3600;
            
            /// <summary>
            /// Short-term cache TTL in seconds (1 minute).
            /// </summary>
            public const int ShortTermTtlSeconds = 60;
            
            /// <summary>
            /// Default Redis key prefix.
            /// </summary>
            public const string DefaultRedisKeyPrefix = "nexo:cache:";
            
            /// <summary>
            /// Default Redis connection string.
            /// </summary>
            public const string DefaultRedisConnectionString = "localhost:6379";
        }

        /// <summary>
        /// AI configuration defaults.
        /// </summary>
        public static class AI
        {
            /// <summary>
            /// Default maximum tokens for AI requests.
            /// </summary>
            public const int DefaultMaxTokens = 1000;
            
            /// <summary>
            /// Default temperature for AI requests.
            /// </summary>
            public const double DefaultTemperature = 0.3;
            
            /// <summary>
            /// Default maximum concurrent AI requests.
            /// </summary>
            public const int DefaultMaxConcurrentRequests = 5;
            
            /// <summary>
            /// Default AI request timeout in seconds.
            /// </summary>
            public const int DefaultRequestTimeoutSeconds = 30;
        }

        /// <summary>
        /// Logging configuration.
        /// </summary>
        public static class Logging
        {
            /// <summary>
            /// Default log level.
            /// </summary>
            public const string DefaultLogLevel = "Information";
            
            /// <summary>
            /// Development log level.
            /// </summary>
            public const string DevelopmentLogLevel = "Debug";
            
            /// <summary>
            /// Production log level.
            /// </summary>
            public const string ProductionLogLevel = "Warning";
        }

        /// <summary>
        /// Environment variable names.
        /// </summary>
        public static class EnvironmentVariables
        {
            /// <summary>
            /// Cache backend environment variable.
            /// </summary>
            public const string CacheBackend = "CACHE_BACKEND";
            
            /// <summary>
            /// Redis connection string environment variable.
            /// </summary>
            public const string RedisConnectionString = "REDIS_CONNECTION_STRING";
            
            /// <summary>
            /// Redis key prefix environment variable.
            /// </summary>
            public const string RedisKeyPrefix = "REDIS_KEY_PREFIX";
            
            /// <summary>
            /// Cache TTL seconds environment variable.
            /// </summary>
            public const string CacheTtlSeconds = "CACHE_TTL_SECONDS";
            
            /// <summary>
            /// AI provider environment variable.
            /// </summary>
            public const string AiProvider = "AI_PROVIDER";
            
            /// <summary>
            /// AI model environment variable.
            /// </summary>
            public const string AiModel = "AI_MODEL";
            
            /// <summary>
            /// OpenAI API key environment variable.
            /// </summary>
            public const string OpenAiApiKey = "OPENAI_API_KEY";
            
            /// <summary>
            /// Azure API key environment variable.
            /// </summary>
            public const string AzureApiKey = "AZURE_API_KEY";
            
            /// <summary>
            /// Azure endpoint environment variable.
            /// </summary>
            public const string AzureEndpoint = "AZURE_ENDPOINT";
        }

        /// <summary>
        /// Default values for configuration.
        /// </summary>
        public static class Defaults
        {
            /// <summary>
            /// Default cache backend.
            /// </summary>
            public const string DefaultCacheBackend = "inmemory";
            
            /// <summary>
            /// Default AI mode.
            /// </summary>
            public const string DefaultAiMode = "development";
            
            /// <summary>
            /// Default execution mode.
            /// </summary>
            public const string DefaultExecutionMode = "development";
            
            /// <summary>
            /// Default optimization level.
            /// </summary>
            public const string DefaultOptimizationLevel = "basic";
            
            /// <summary>
            /// Default analysis depth.
            /// </summary>
            public const string DefaultAnalysisDepth = "surface";
            
            /// <summary>
            /// Default focus areas.
            /// </summary>
            public const string DefaultFocusAreas = "all";
        }
    }
} 