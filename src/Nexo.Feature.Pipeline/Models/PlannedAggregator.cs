using System;
using System.Collections.Generic;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents an aggregator in an execution plan.
    /// </summary>
    public class PlannedAggregator
    {
        /// <summary>
        /// Gets or sets the aggregator ID.
        /// </summary>
        public string AggregatorId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregator name.
        /// </summary>
        public string AggregatorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the aggregator category.
        /// </summary>
        public AggregatorCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the execution strategy.
        /// </summary>
        public AggregatorExecutionStrategy ExecutionStrategy { get; set; }

        /// <summary>
        /// Gets or sets whether the aggregator can execute in parallel.
        /// </summary>
        public bool CanExecuteInParallel { get; set; }

        /// <summary>
        /// Gets or sets the dependencies for this aggregator.
        /// </summary>
        public List<AggregatorDependency> Dependencies { get; set; } = new List<AggregatorDependency>();

        /// <summary>
        /// Gets or sets the estimated execution time in milliseconds.
        /// </summary>
        public long EstimatedExecutionTimeMs { get; set; }
    }
} 