using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Execution pattern for learning and analysis.
    /// </summary>
    public partial class ExecutionPattern
    {
        /// <summary>
        /// Gets or sets the execution identifier.
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution patterns.
        /// </summary>
        public Dictionary<string, object> Patterns { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets whether the execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this pattern was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
