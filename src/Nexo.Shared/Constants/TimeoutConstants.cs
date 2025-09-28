namespace Nexo.Shared
{
    /// <summary>
    /// Default timeout values in milliseconds.
    /// </summary>
    public static class TimeoutConstants
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
}
