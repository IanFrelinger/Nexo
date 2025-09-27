using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Result classes for test orchestration
    /// </summary>
    public partial class TestOrchestrationResult
    {
        /// <summary>
        /// Whether the orchestration was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Total number of tests executed.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Number of tests that passed.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests that failed.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests that were skipped.
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Parallel execution metrics.
        /// </summary>
        public ParallelExecutionMetrics ParallelMetrics { get; set; } = new ParallelExecutionMetrics();

        /// <summary>
        /// Resource utilization during execution.
        /// </summary>
        public ResourceUtilization ResourceUtilization { get; set; } = new ResourceUtilization();

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> TestResults { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Warnings or issues encountered.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Error message if orchestration failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of parallel execution.
    /// </summary>
    public partial class ParallelExecutionResult
    {
        /// <summary>
        /// Whether parallel execution was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Number of tests executed in parallel.
        /// </summary>
        public int TestsExecuted { get; set; }

        /// <summary>
        /// Parallel execution metrics.
        /// </summary>
        public ParallelExecutionMetrics Metrics { get; set; } = new ParallelExecutionMetrics();

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> Results { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Error message if execution failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of incremental testing.
    /// </summary>
    public partial class IncrementalTestingResult
    {
        /// <summary>
        /// Whether incremental testing was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Number of tests executed incrementally.
        /// </summary>
        public int TestsExecuted { get; set; }

        /// <summary>
        /// Number of tests that would have been executed in full suite.
        /// </summary>
        public int TotalTestsInSuite { get; set; }

        /// <summary>
        /// Time saved compared to full suite execution.
        /// </summary>
        public TimeSpan TimeSaved { get; set; }

        /// <summary>
        /// Confidence level of the incremental selection.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> Results { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Whether fallback to full suite was used.
        /// </summary>
        public bool UsedFallback { get; set; }

        /// <summary>
        /// Error message if incremental testing failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
