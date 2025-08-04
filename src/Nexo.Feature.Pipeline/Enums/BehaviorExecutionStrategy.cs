namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Execution strategies for commands within behaviors.
    /// </summary>
    public enum BehaviorExecutionStrategy
    {
        /// <summary>
        /// Commands execute sequentially in the order they are defined.
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Commands execute in parallel where possible.
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Commands execute conditionally based on previous command results.
        /// </summary>
        Conditional,
        
        /// <summary>
        /// Commands execute in batches with parallel execution within batches.
        /// </summary>
        Batched,
        
        /// <summary>
        /// Commands execute with retry logic on failure.
        /// </summary>
        Retry,
        
        /// <summary>
        /// Commands execute with fallback options on failure.
        /// </summary>
        Fallback,
        
        /// <summary>
        /// Commands execute with custom orchestration logic.
        /// </summary>
        Custom
    }
} 