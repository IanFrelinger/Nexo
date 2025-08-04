namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Execution strategies for behaviors within aggregators.
    /// </summary>
    public enum AggregatorExecutionStrategy
    {
        /// <summary>
        /// Behaviors execute sequentially in the order they are defined.
        /// </summary>
        Sequential,
        
        /// <summary>
        /// Behaviors execute in parallel where possible.
        /// </summary>
        Parallel,
        
        /// <summary>
        /// Behaviors execute conditionally based on previous behavior results.
        /// </summary>
        Conditional,
        
        /// <summary>
        /// Behaviors execute in phases with parallel execution within phases.
        /// </summary>
        Phased,
        
        /// <summary>
        /// Behaviors execute with dependency resolution and ordering.
        /// </summary>
        DependencyOrdered,
        
        /// <summary>
        /// Behaviors execute with resource-aware scheduling.
        /// </summary>
        ResourceAware,
        
        /// <summary>
        /// Behaviors execute with custom orchestration logic.
        /// </summary>
        Custom
    }
} 