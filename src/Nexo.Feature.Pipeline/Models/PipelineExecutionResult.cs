using System;
using System.Collections.Generic;
using Nexo.Shared.Models;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of a pipeline execution.
    /// </summary>
    public class PipelineExecutionResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for this execution.
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the execution was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the execution status.
        /// </summary>
        public ExecutionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message if execution failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the start time of the execution.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the execution.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the total execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the execution plan that was used.
        /// </summary>
        public PipelineExecutionPlan ExecutionPlan { get; set; } = new PipelineExecutionPlan();

        /// <summary>
        /// Gets or sets the results from individual aggregators.
        /// </summary>
        public List<AggregatorResult> AggregatorResults { get; set; } = new List<AggregatorResult>();

        /// <summary>
        /// Gets or sets the results from individual behaviors.
        /// </summary>
        public List<BehaviorResult> BehaviorResults { get; set; } = new List<BehaviorResult>();

        /// <summary>
        /// Gets or sets the execution metrics.
        /// </summary>
        public List<ExecutionMetric> Metrics { get; set; } = new List<ExecutionMetric>();

        /// <summary>
        /// Gets or sets validation errors if any occurred.
        /// </summary>
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
    }
} 