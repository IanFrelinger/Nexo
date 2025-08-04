using System;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Daily trend data point.
    /// </summary>
    public class DailyTrendPoint
    {
        /// <summary>
        /// Date for this trend point.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Number of test runs on this date.
        /// </summary>
        public int TestRuns { get; set; }

        /// <summary>
        /// Success rate for this date.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Average execution time in milliseconds.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Number of passed tests.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of failed tests.
        /// </summary>
        public int FailedTests { get; set; }
    }
} 