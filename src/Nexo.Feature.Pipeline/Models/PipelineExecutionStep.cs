using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a single execution step in the pipeline.
    /// </summary>
    public class PipelineExecutionStep
    {
        /// <summary>
        /// Unique identifier for this execution step.
        /// </summary>
        public string StepId { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of the execution step.
        /// </summary>
        public ExecutionStepType StepType { get; set; }
        
        /// <summary>
        /// Name of the command, behavior, or aggregator being executed.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the command, behavior, or aggregator being executed.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Status of this execution step.
        /// </summary>
        public ExecutionStepStatus Status { get; set; }
        
        /// <summary>
        /// Timestamp when this step started.
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Timestamp when this step completed.
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Duration of this step in milliseconds.
        /// </summary>
        public long DurationMs { get { return (EndTime > StartTime) ? (long)(EndTime - StartTime).TotalMilliseconds : 0; } }
        
        /// <summary>
        /// Error message if this step failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception that occurred during this step, if any.
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Additional metadata about this step.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Child execution steps (for behaviors and aggregators).
        /// </summary>
        public List<PipelineExecutionStep> ChildSteps { get; set; } = new List<PipelineExecutionStep>();
        
        /// <summary>
        /// Whether this step was executed in parallel.
        /// </summary>
        public bool WasExecutedInParallel { get; set; }
        
        /// <summary>
        /// Number of retries performed for this step.
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// Priority of this step.
        /// </summary>
        public CommandPriority Priority { get; set; }
        
        /// <summary>
        /// Creates a new execution step.
        /// </summary>
        /// <param name="stepType">Type of the execution step.</param>
        /// <param name="name">Name of the step.</param>
        /// <param name="id">ID of the step.</param>
        /// <param name="priority">Priority of the step.</param>
        /// <returns>A new execution step.</returns>
        public static PipelineExecutionStep Create(ExecutionStepType stepType, string name, string id, CommandPriority priority = CommandPriority.Normal)
        {
            return new PipelineExecutionStep
            {
                StepId = Guid.NewGuid().ToString(),
                StepType = stepType,
                Name = name,
                Id = id,
                Priority = priority,
                Status = ExecutionStepStatus.NotStarted,
                StartTime = DateTime.MinValue,
                EndTime = DateTime.MinValue
            };
        }
        
        /// <summary>
        /// Marks this step as started.
        /// </summary>
        public void MarkStarted()
        {
            Status = ExecutionStepStatus.Executing;
            StartTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Marks this step as completed successfully.
        /// </summary>
        public void MarkCompleted()
        {
            Status = ExecutionStepStatus.Completed;
            EndTime = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Marks this step as failed.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that occurred.</param>
        public void MarkFailed(string errorMessage, Exception exception)
        {
            Status = ExecutionStepStatus.Failed;
            EndTime = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
        
        /// <summary>
        /// Adds metadata to this step.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }
        
        /// <summary>
        /// Adds a child step to this step.
        /// </summary>
        /// <param name="childStep">The child step to add.</param>
        public void AddChildStep(PipelineExecutionStep childStep)
        {
            ChildSteps.Add(childStep);
        }
    }

    /// <summary>
    /// Type of execution step.
    /// </summary>
    public enum ExecutionStepType
    {
        /// <summary>
        /// Command execution step.
        /// </summary>
        Command,
        
        /// <summary>
        /// Behavior execution step.
        /// </summary>
        Behavior,
        
        /// <summary>
        /// Aggregator execution step.
        /// </summary>
        Aggregator
    }

    /// <summary>
    /// Status of an execution step.
    /// </summary>
    public enum ExecutionStepStatus
    {
        /// <summary>
        /// Step has not started.
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// Step is executing.
        /// </summary>
        Executing,
        
        /// <summary>
        /// Step completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// Step failed.
        /// </summary>
        Failed,
        
        /// <summary>
        /// Step was skipped.
        /// </summary>
        Skipped,
        
        /// <summary>
        /// Step was cancelled.
        /// </summary>
        Cancelled
    }
} 