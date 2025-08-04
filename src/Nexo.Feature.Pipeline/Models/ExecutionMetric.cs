using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a performance metric for pipeline execution.
    /// </summary>
    public class ExecutionMetric
    {
        /// <summary>
        /// Gets or sets the category of the metric (e.g., "Phase", "Aggregator", "Behavior").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the metric.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the count of items processed.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the metric was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the metric.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 