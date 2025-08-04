using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Analysis of test failures and patterns.
    /// </summary>
    public class TestFailureAnalysis
    {
        /// <summary>
        /// Patterns identified in test failures.
        /// </summary>
        public List<FailurePattern> FailurePatterns { get; set; } = new List<FailurePattern>();

        /// <summary>
        /// Groups of flaky tests.
        /// </summary>
        public List<TestGroup> FlakyTestGroups { get; set; } = new List<TestGroup>();

        /// <summary>
        /// Failure counts by environment.
        /// </summary>
        public Dictionary<string, int> EnvironmentFailures { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Time-based failure patterns.
        /// </summary>
        public List<TimeBasedPattern> TimeBasedPatterns { get; set; } = new List<TimeBasedPattern>();

        /// <summary>
        /// Suggested root causes for failures.
        /// </summary>
        public List<string> RootCauseSuggestions { get; set; } = new List<string>();
    }
} 