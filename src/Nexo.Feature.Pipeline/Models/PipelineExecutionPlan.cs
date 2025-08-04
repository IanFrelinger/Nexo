using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a planned pipeline execution with phases and dependencies.
    /// </summary>
    public class PipelineExecutionPlan
    {
        /// <summary>
        /// Gets or sets the unique identifier for this execution plan.
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when this plan was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Gets or sets the estimated execution time in milliseconds.
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the execution phases.
        /// </summary>
        public List<PipelineExecutionPhase> Phases { get; set; } = new List<PipelineExecutionPhase>();

        /// <summary>
        /// Gets or sets the dependencies between components.
        /// </summary>
        public List<ExecutionDependency> Dependencies { get; set; } = new List<ExecutionDependency>();
    }
} 