namespace Nexo.Shared
{
    /// <summary>
    /// Default limits and thresholds.
    /// </summary>
    public static class LimitConstants
    {
        /// <summary>
        /// Default maximum number of concurrent operations.
        /// </summary>
        public const int DefaultMaxConcurrentOperations = 10;
        
        /// <summary>
        /// Default maximum number of concurrent HTTP requests.
        /// </summary>
        public const int DefaultMaxConcurrentHttpRequests = 5;
        
        /// <summary>
        /// Default maximum number of items in a batch.
        /// </summary>
        public const int DefaultMaxBatchSize = 100;
        
        /// <summary>
        /// Default maximum number of items in a collection.
        /// </summary>
        public const int DefaultMaxCollectionSize = 1000;
        
        /// <summary>
        /// Default maximum number of retry attempts.
        /// </summary>
        public const int DefaultMaxRetryAttempts = 3;
        
        /// <summary>
        /// Default maximum number of items in a queue.
        /// </summary>
        public const int DefaultMaxQueueSize = 10000;
    }
}
