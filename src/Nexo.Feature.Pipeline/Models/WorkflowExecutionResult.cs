using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Result of a workflow execution.
    /// </summary>
    public class WorkflowExecutionResult
    {
        /// <summary>
        /// Type of workflow that was executed.
        /// </summary>
        public WorkflowType WorkflowType { get; set; }

        /// <summary>
        /// Path to the project that was processed.
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// Configuration used for the workflow.
        /// </summary>
        public WorkflowConfiguration Configuration { get; set; } = new WorkflowConfiguration();

        /// <summary>
        /// Overall status of the workflow execution.
        /// </summary>
        public WorkflowExecutionStatus Status { get; set; }

        /// <summary>
        /// Start time of the workflow execution.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the workflow execution.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration of the workflow execution.
        /// </summary>
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);

        /// <summary>
        /// List of step results from the workflow execution.
        /// </summary>
        public List<WorkflowStepResult> StepResults { get; set; } = new List<WorkflowStepResult>();

        /// <summary>
        /// Any error messages from the workflow execution.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Any warning messages from the workflow execution.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Additional metadata about the workflow execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the number of successful steps.
        /// </summary>
        public int SuccessfulSteps => StepResults.Count(s => s.Status == WorkflowStepStatus.Completed);

        /// <summary>
        /// Gets the number of failed steps.
        /// </summary>
        public int FailedSteps => StepResults.Count(s => s.Status == WorkflowStepStatus.Failed);

        /// <summary>
        /// Gets the number of skipped steps.
        /// </summary>
        public int SkippedSteps => StepResults.Count(s => s.Status == WorkflowStepStatus.Skipped);

        /// <summary>
        /// Gets whether the workflow execution was successful.
        /// </summary>
        public bool IsSuccess => Status == WorkflowExecutionStatus.Completed && FailedSteps == 0;

        /// <summary>
        /// Gets the error message if the workflow failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets the total number of steps.
        /// </summary>
        public int TotalSteps => StepResults.Count;

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalSteps > 0 ? (double)SuccessfulSteps / TotalSteps * 100 : 0;
    }

    /// <summary>
    /// Status of workflow execution.
    /// </summary>
    public enum WorkflowExecutionStatus
    {
        /// <summary>
        /// Workflow is running.
        /// </summary>
        Running,

        /// <summary>
        /// Workflow completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Workflow failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Workflow was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Workflow timed out.
        /// </summary>
        TimedOut
    }

    /// <summary>
    /// Result of a workflow step execution.
    /// </summary>
    public class WorkflowStepResult
    {
        /// <summary>
        /// Name of the step that was executed.
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// The step that was executed.
        /// </summary>
        public WorkflowStep Step { get; set; } = new WorkflowStep();

        /// <summary>
        /// Status of the step execution.
        /// </summary>
        public WorkflowStepStatus Status { get; set; }

        /// <summary>
        /// Exit code of the step (if applicable).
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Standard output from the step.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Standard error from the step.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Error message if the step failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets whether the step execution was successful.
        /// </summary>
        public bool IsSuccess => Status == WorkflowStepStatus.Completed;

        /// <summary>
        /// Start time of the step execution.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the step execution.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the step execution.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Number of retry attempts made for this step.
        /// </summary>
        public int RetryAttempts { get; set; }
    }

    /// <summary>
    /// Status of workflow step execution.
    /// </summary>
    public enum WorkflowStepStatus
    {
        /// <summary>
        /// Step is running.
        /// </summary>
        Running,

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
        Cancelled,

        /// <summary>
        /// Step timed out.
        /// </summary>
        TimedOut
    }
}