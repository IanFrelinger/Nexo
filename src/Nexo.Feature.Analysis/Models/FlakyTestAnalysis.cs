using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Analysis of flaky test behavior.
    /// </summary>
    public class FlakyTestAnalysis
    {
        /// <summary>
        /// Name of the flaky test.
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Percentage of flakiness (0-100).
        /// </summary>
        public double FlakinessPercentage { get; set; }

        /// <summary>
        /// Total number of test runs.
        /// </summary>
        public int TotalRuns { get; set; }

        /// <summary>
        /// Number of inconsistent runs.
        /// </summary>
        public int InconsistentRuns { get; set; }

        /// <summary>
        /// Patterns in test failures.
        /// </summary>
        public List<string> FailurePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Suggested fixes for the flaky test.
        /// </summary>
        public List<string> SuggestedFixes { get; set; } = new List<string>();
    }
} 