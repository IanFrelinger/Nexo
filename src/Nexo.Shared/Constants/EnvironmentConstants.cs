namespace Nexo.Shared
{
    /// <summary>
    /// Default environment variable names.
    /// </summary>
    public static class EnvironmentConstants
    {
        /// <summary>
        /// Environment variable for API key.
        /// </summary>
        public const string ApiKeyEnvironmentVariable = "NEXO_API_KEY";
        
        /// <summary>
        /// Environment variable for database connection string.
        /// </summary>
        public const string DatabaseConnectionStringEnvironmentVariable = "NEXO_DATABASE_CONNECTION_STRING";
        
        /// <summary>
        /// Environment variable for log level.
        /// </summary>
        public const string LogLevelEnvironmentVariable = "NEXO_LOG_LEVEL";
        
        /// <summary>
        /// Environment variable for cache size.
        /// </summary>
        public const string CacheSizeEnvironmentVariable = "NEXO_CACHE_SIZE";
        
        /// <summary>
        /// Environment variable for timeout.
        /// </summary>
        public const string TimeoutEnvironmentVariable = "NEXO_TIMEOUT";
    }
}
