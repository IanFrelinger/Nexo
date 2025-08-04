using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Monitors test execution in real-time.
    /// </summary>
    public class TestExecutionMonitor
    {
        /// <summary>
        /// Unique identifier for this monitoring session.
        /// </summary>
        public string RunId { get; set; } = string.Empty;

        /// <summary>
        /// Environment being monitored.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Project being tested.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Current execution status.
        /// </summary>
        public TestExecutionStatus Status { get; set; }

        /// <summary>
        /// Start time of the monitoring session.
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End time of the monitoring session.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs => EndTime.HasValue ? (long)(EndTime.Value - StartTime).TotalMilliseconds : (long)(DateTime.UtcNow - StartTime).TotalMilliseconds;

        /// <summary>
        /// Number of tests completed.
        /// </summary>
        public int CompletedTests { get; set; }

        /// <summary>
        /// Number of tests passed.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests failed.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests skipped.
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Total number of tests to execute.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public double ProgressPercentage => TotalTests > 0 ? (double)CompletedTests / TotalTests * 100 : 0;

        /// <summary>
        /// Success rate percentage.
        /// </summary>
        public double SuccessRate => CompletedTests > 0 ? (double)PassedTests / CompletedTests * 100 : 0;

        /// <summary>
        /// Current test being executed.
        /// </summary>
        public string CurrentTest { get; set; } = string.Empty;

        /// <summary>
        /// Recent test results.
        /// </summary>
        public List<DetailedTestResult> RecentResults { get; set; } = new List<DetailedTestResult>();

        /// <summary>
        /// Performance metrics.
        /// </summary>
        public TestPerformanceMetrics PerformanceMetrics { get; set; } = new TestPerformanceMetrics();

        /// <summary>
        /// Warnings and alerts.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Total number of tests executed.
        /// </summary>
        public int TotalTestsExecuted => CompletedTests;

        /// <summary>
        /// Total number of tests passed.
        /// </summary>
        public int TotalTestsPassed => PassedTests;

        /// <summary>
        /// Total number of tests failed.
        /// </summary>
        public int TotalTestsFailed => FailedTests;

        /// <summary>
        /// Total number of tests skipped.
        /// </summary>
        public int TotalTestsSkipped => SkippedTests;

        /// <summary>
        /// Updates the status of the monitor.
        /// </summary>
        /// <param name="status">New status.</param>
        public void UpdateStatus(TestExecutionStatus status)
        {
            Status = status;
            if (status == TestExecutionStatus.Completed || status == TestExecutionStatus.Failed || status == TestExecutionStatus.Timeout)
            {
                EndTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Adds a test result to the monitor.
        /// </summary>
        /// <param name="result">Test result to add.</param>
        public void AddTestResult(DetailedTestResult result)
        {
            CompletedTests++;
            
            switch (result.Status)
            {
                case TestStatus.Passed:
                    PassedTests++;
                    break;
                case TestStatus.Failed:
                    FailedTests++;
                    break;
                case TestStatus.Skipped:
                    SkippedTests++;
                    break;
            }

            RecentResults.Add(result);
            
            // Keep only the last 100 results
            if (RecentResults.Count > 100)
            {
                RecentResults.RemoveAt(0);
            }
        }

        /// <summary>
        /// Updates the current test being executed.
        /// </summary>
        /// <param name="testName">Name of the current test.</param>
        public void UpdateCurrentTest(string testName)
        {
            CurrentTest = testName;
        }

        /// <summary>
        /// Records a test result (alias for AddTestResult for compatibility).
        /// </summary>
        /// <param name="result">Test result to add.</param>
        public void RecordTestResult(DetailedTestResult result)
        {
            AddTestResult(result);
        }
    }
}