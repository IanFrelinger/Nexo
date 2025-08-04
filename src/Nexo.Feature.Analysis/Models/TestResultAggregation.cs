using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Comprehensive test result aggregation with historical data and analytics.
    /// </summary>
    public class TestResultAggregation
    {
        /// <summary>
        /// Unique identifier for this test run.
        /// </summary>
        public string RunId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the test run started.
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the test run completed.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Environment where tests were executed.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Project or solution being tested.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Overall test execution status.
        /// </summary>
        public TestExecutionStatus Status { get; set; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Test results by category.
        /// </summary>
        public Dictionary<string, TestCategoryResult> CategoryResults { get; set; } = new Dictionary<string, TestCategoryResult>();

        /// <summary>
        /// Individual test results.
        /// </summary>
        public List<DetailedTestResult> TestResults { get; set; } = new List<DetailedTestResult>();

        /// <summary>
        /// Performance metrics for this test run.
        /// </summary>
        public TestPerformanceMetrics PerformanceMetrics { get; set; } = new TestPerformanceMetrics();

        /// <summary>
        /// Failure analysis and patterns.
        /// </summary>
        public TestFailureAnalysis FailureAnalysis { get; set; } = new TestFailureAnalysis();

        /// <summary>
        /// Coverage information if available.
        /// </summary>
        public TestCoverageInfo CoverageInfo { get; set; } = new TestCoverageInfo();

        /// <summary>
        /// Metadata about the test run.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Warnings generated during test execution.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Total number of tests executed.
        /// </summary>
        public int TotalTests => TestResults.Count;

        /// <summary>
        /// Number of tests that passed.
        /// </summary>
        public int PassedTests => TestResults.Count(t => t.Status == TestStatus.Passed);

        /// <summary>
        /// Number of tests that failed.
        /// </summary>
        public int FailedTests => TestResults.Count(t => t.Status == TestStatus.Failed);

        /// <summary>
        /// Number of tests that were skipped.
        /// </summary>
        public int SkippedTests => TestResults.Count(t => t.Status == TestStatus.Skipped);

        /// <summary>
        /// Success rate percentage.
        /// </summary>
        public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

        /// <summary>
        /// Failure rate percentage.
        /// </summary>
        public double FailureRate => TotalTests > 0 ? (double)FailedTests / TotalTests * 100 : 0;

        /// <summary>
        /// Average test execution time.
        /// </summary>
        public double AverageTestTimeMs => TotalTests > 0 ? (double)ExecutionTimeMs / TotalTests : 0;

        /// <summary>
        /// Whether all tests passed.
        /// </summary>
        public bool AllTestsPassed => FailedTests == 0 && TotalTests > 0;
    }
}