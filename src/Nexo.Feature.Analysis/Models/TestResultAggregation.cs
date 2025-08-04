using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

        /// <summary>
        /// Failure rate as a percentage.
        /// </summary>
        public double FailureRate => TotalTests > 0 ? (double)FailedTests / TotalTests * 100 : 0;

        /// <summary>
        /// Average test execution time in milliseconds.
        /// </summary>
        public double AverageTestTimeMs => TotalTests > 0 ? (double)ExecutionTimeMs / TotalTests : 0;

        /// <summary>
        /// Whether all tests passed.
        /// </summary>
        public bool AllTestsPassed => FailedTests == 0 && TotalTests > 0;
    }

    /// <summary>
    /// Test execution status.
    /// </summary>
    public enum TestExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Cancelled,
        Timeout
    }

    /// <summary>
    /// Individual test status.
    /// </summary>
    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Inconclusive
    }

    /// <summary>
    /// Detailed result for an individual test.
    /// </summary>
    public class DetailedTestResult
    {
        /// <summary>
        /// Test name.
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Test class or category.
        /// </summary>
        public string TestClass { get; set; } = string.Empty;

        /// <summary>
        /// Test status.
        /// </summary>
        public TestStatus Status { get; set; }

        /// <summary>
        /// Test execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Error message if the test failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace if the test failed.
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Test output.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Test metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Test results grouped by category.
    /// </summary>
    public class TestCategoryResult
    {
        /// <summary>
        /// Category name.
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Total tests in this category.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Passed tests in this category.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Failed tests in this category.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Skipped tests in this category.
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Total execution time for this category.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Success rate for this category.
        /// </summary>
        public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    }

    /// <summary>
    /// Performance metrics for test execution.
    /// </summary>
    public class TestPerformanceMetrics
    {
        /// <summary>
        /// Memory usage at start in bytes.
        /// </summary>
        public long MemoryUsageStartBytes { get; set; }

        /// <summary>
        /// Memory usage at end in bytes.
        /// </summary>
        public long MemoryUsageEndBytes { get; set; }

        /// <summary>
        /// Peak memory usage during execution in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// CPU usage percentage during execution.
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// Number of parallel test executions.
        /// </summary>
        public int ParallelExecutions { get; set; }

        /// <summary>
        /// Slowest test execution time in milliseconds.
        /// </summary>
        public long SlowestTestTimeMs { get; set; }

        /// <summary>
        /// Fastest test execution time in milliseconds.
        /// </summary>
        public long FastestTestTimeMs { get; set; }

        /// <summary>
        /// Average test execution time in milliseconds.
        /// </summary>
        public double AverageTestTimeMs { get; set; }

        /// <summary>
        /// Tests that took longer than expected (outliers).
        /// </summary>
        public List<string> SlowTests { get; set; } = new List<string>();
    }

    /// <summary>
    /// Analysis of test failures and patterns.
    /// </summary>
    public class TestFailureAnalysis
    {
        /// <summary>
        /// Common failure patterns identified.
        /// </summary>
        public List<FailurePattern> FailurePatterns { get; set; } = new List<FailurePattern>();

        /// <summary>
        /// Tests that frequently fail together.
        /// </summary>
        public List<TestGroup> FlakyTestGroups { get; set; } = new List<TestGroup>();

        /// <summary>
        /// Environment-specific failures.
        /// </summary>
        public Dictionary<string, int> EnvironmentFailures { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Time-based failure patterns.
        /// </summary>
        public List<TimeBasedPattern> TimeBasedPatterns { get; set; } = new List<TimeBasedPattern>();

        /// <summary>
        /// Root cause analysis suggestions.
        /// </summary>
        public List<string> RootCauseSuggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Identified failure pattern.
    /// </summary>
    public class FailurePattern
    {
        /// <summary>
        /// Pattern name or description.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Pattern type.
        /// </summary>
        public FailurePatternType Type { get; set; }

        /// <summary>
        /// Tests affected by this pattern.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();

        /// <summary>
        /// Frequency of this pattern.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Confidence level (0-100).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Suggested fixes.
        /// </summary>
        public List<string> SuggestedFixes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Type of failure pattern.
    /// </summary>
    public enum FailurePatternType
    {
        Timeout,
        ResourceExhaustion,
        EnvironmentIssue,
        CodeIssue,
        FlakyTest,
        DependencyIssue,
        ConfigurationIssue
    }

    /// <summary>
    /// Group of tests that fail together.
    /// </summary>
    public class TestGroup
    {
        /// <summary>
        /// Group name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tests in this group.
        /// </summary>
        public List<string> Tests { get; set; } = new List<string>();

        /// <summary>
        /// Failure rate for this group.
        /// </summary>
        public double FailureRate { get; set; }

        /// <summary>
        /// Common error patterns in this group.
        /// </summary>
        public List<string> CommonErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Time-based failure pattern.
    /// </summary>
    public class TimeBasedPattern
    {
        /// <summary>
        /// Pattern description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Time range when failures occur.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Time range when failures occur.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Day of week pattern.
        /// </summary>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Affected tests.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();
    }

    /// <summary>
    /// Test coverage information.
    /// </summary>
    public class TestCoverageInfo
    {
        /// <summary>
        /// Overall code coverage percentage.
        /// </summary>
        public double OverallCoverage { get; set; }

        /// <summary>
        /// Line coverage percentage.
        /// </summary>
        public double LineCoverage { get; set; }

        /// <summary>
        /// Branch coverage percentage.
        /// </summary>
        public double BranchCoverage { get; set; }

        /// <summary>
        /// Method coverage percentage.
        /// </summary>
        public double MethodCoverage { get; set; }

        /// <summary>
        /// Coverage by assembly or module.
        /// </summary>
        public Dictionary<string, double> CoverageByModule { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Uncovered code areas.
        /// </summary>
        public List<string> UncoveredAreas { get; set; } = new List<string>();
    }
}