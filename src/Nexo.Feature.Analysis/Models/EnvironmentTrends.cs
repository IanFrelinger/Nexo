using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Environment-specific trend analysis.
    /// </summary>
    public class EnvironmentTrends
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Success rate trend for this environment.
        /// </summary>
        public TrendData SuccessRateTrend { get; set; } = new TrendData();

        /// <summary>
        /// Execution time trend for this environment.
        /// </summary>
        public TrendData ExecutionTimeTrend { get; set; } = new TrendData();

        /// <summary>
        /// Failure patterns specific to this environment.
        /// </summary>
        public List<FailurePattern> FailurePatterns { get; set; } = new List<FailurePattern>();
    }
} 