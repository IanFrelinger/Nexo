namespace Nexo.Shared
{
    /// <summary>
    /// Default logging configuration.
    /// </summary>
    public static class LoggingConstants
    {
        /// <summary>
        /// Default log level.
        /// </summary>
        public const string DefaultLogLevel = "Information";
        
        /// <summary>
        /// Default log file size limit in MB.
        /// </summary>
        public const int DefaultLogFileSizeLimitMB = 10;
        
        /// <summary>
        /// Default log retention days.
        /// </summary>
        public const int DefaultLogRetentionDays = 30;
        
        /// <summary>
        /// Default log buffer size.
        /// </summary>
        public const int DefaultLogBufferSize = 1000;
        
        /// <summary>
        /// Default log flush interval in seconds.
        /// </summary>
        public const int DefaultLogFlushIntervalSeconds = 5;
    }
}
