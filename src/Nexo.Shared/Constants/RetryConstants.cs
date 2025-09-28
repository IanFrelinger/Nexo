namespace Nexo.Shared
{
    /// <summary>
    /// Default retry configuration.
    /// </summary>
    public static class RetryConstants
    {
        /// <summary>
        /// Default maximum number of retries.
        /// </summary>
        public const int DefaultMaxRetries = 3;
        
        /// <summary>
        /// Default maximum retries for critical operations.
        /// </summary>
        public const int DefaultMaxRetriesCritical = 5;
        
        /// <summary>
        /// Default maximum retries for non-critical operations.
        /// </summary>
        public const int DefaultMaxRetriesNonCritical = 1;
        
        /// <summary>
        /// Default exponential backoff multiplier.
        /// </summary>
        public const double DefaultBackoffMultiplier = 2.0;
        
        /// <summary>
        /// Default maximum backoff delay in milliseconds.
        /// </summary>
        public const int DefaultMaxBackoffDelayMs = 30000;
    }
}
