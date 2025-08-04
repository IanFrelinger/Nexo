namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Priority levels for command execution ordering.
    /// </summary>
    public enum CommandPriority
    {
        /// <summary>
        /// Critical priority - must execute first.
        /// </summary>
        Critical = 0,
        
        /// <summary>
        /// High priority - executes early in the pipeline.
        /// </summary>
        High = 1,
        
        /// <summary>
        /// Normal priority - default execution order.
        /// </summary>
        Normal = 2,
        
        /// <summary>
        /// Low priority - executes later in the pipeline.
        /// </summary>
        Low = 3,
        
        /// <summary>
        /// Background priority - executes when resources are available.
        /// </summary>
        Background = 4
    }
} 