namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Status of pipeline execution.
    /// </summary>
    public enum PipelineExecutionStatus
    {
        /// <summary>
        /// Pipeline is not started.
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// Pipeline is being initialized.
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Pipeline is validating.
        /// </summary>
        Validating,
        
        /// <summary>
        /// Pipeline is planning execution.
        /// </summary>
        Planning,
        
        /// <summary>
        /// Pipeline is executing.
        /// </summary>
        Executing,
        
        /// <summary>
        /// Pipeline is paused.
        /// </summary>
        Paused,
        
        /// <summary>
        /// Pipeline is being cancelled.
        /// </summary>
        Cancelling,
        
        /// <summary>
        /// Pipeline execution completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// Pipeline execution failed.
        /// </summary>
        Failed,
        
        /// <summary>
        /// Pipeline execution was cancelled.
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// Pipeline is being cleaned up.
        /// </summary>
        CleaningUp
    }
} 