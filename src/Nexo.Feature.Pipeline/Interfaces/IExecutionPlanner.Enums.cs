namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Execution strategy for pipeline processing.
    /// </summary>
    public enum ExecutionStrategy
    {
        /// <summary>
        /// Sequential execution.
        /// </summary>
        Sequential,

        /// <summary>
        /// Parallel execution.
        /// </summary>
        Parallel,

        /// <summary>
        /// Hybrid execution (mix of sequential and parallel).
        /// </summary>
        Hybrid,

        /// <summary>
        /// Adaptive execution (strategy changes based on conditions).
        /// </summary>
        Adaptive
    }

    /// <summary>
    /// Types of execution steps.
    /// </summary>
    public enum ExecutionStepType
    {
        /// <summary>
        /// Data processing step.
        /// </summary>
        DataProcessing,

        /// <summary>
        /// Validation step.
        /// </summary>
        Validation,

        /// <summary>
        /// Transformation step.
        /// </summary>
        Transformation,

        /// <summary>
        /// Aggregation step.
        /// </summary>
        Aggregation,

        /// <summary>
        /// Filtering step.
        /// </summary>
        Filtering,

        /// <summary>
        /// Sorting step.
        /// </summary>
        Sorting,

        /// <summary>
        /// Custom step.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Step priority levels.
    /// </summary>
    public enum StepPriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Retry strategies for execution steps.
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// Fixed delay between retries.
        /// </summary>
        FixedDelay,

        /// <summary>
        /// Exponential backoff delay.
        /// </summary>
        ExponentialBackoff,

        /// <summary>
        /// Linear backoff delay.
        /// </summary>
        LinearBackoff,

        /// <summary>
        /// Custom retry strategy.
        /// </summary>
        Custom
    }
}
