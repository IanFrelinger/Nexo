namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Defines the status of pipeline execution.
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// Execution is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// Execution is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Execution completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Execution failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Execution was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Execution is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Execution is waiting for dependencies.
        /// </summary>
        Waiting
    }
} 