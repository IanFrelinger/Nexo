using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Real-time statistics for test execution.
    /// </summary>
    public class TestExecutionStatistics
    {
        /// <summary>
        /// Timestamp when these statistics were generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total number of active test runs.
        /// </summary>
        public int ActiveTestRuns { get; set; }

        /// <summary>
        /// Total number of active executions (alias for ActiveTestRuns).
        /// </summary>
        public int ActiveExecutions => ActiveTestRuns;

        /// <summary>
        /// Total number of tests completed across all runs.
        /// </summary>
        public int TotalTestsCompleted { get; set; }

        /// <summary>
        /// Total number of tests executed (alias for TotalTestsCompleted).
        /// </summary>
        public int TotalTestsExecuted => TotalTestsCompleted;

        /// <summary>
        /// Total number of tests passed across all runs.
        /// </summary>
        public int TotalTestsPassed { get; set; }

        /// <summary>
        /// Total number of tests failed across all runs.
        /// </summary>
        public int TotalTestsFailed { get; set; }

        /// <summary>
        /// Total number of tests skipped across all runs.
        /// </summary>
        public int TotalTestsSkipped { get; set; }

        /// <summary>
        /// Overall success rate percentage.
        /// </summary>
        public double OverallSuccessRate => TotalTestsCompleted > 0 ? (double)TotalTestsPassed / TotalTestsCompleted * 100 : 0;

        /// <summary>
        /// Average test execution time in milliseconds.
        /// </summary>
        public double AverageTestTimeMs { get; set; }

        /// <summary>
        /// Average execution time in milliseconds (alias for AverageTestTimeMs).
        /// </summary>
        public double AverageExecutionTimeMs => AverageTestTimeMs;

        /// <summary>
        /// Total execution time across all runs in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// Memory usage statistics.
        /// </summary>
        public MemoryStatistics MemoryStats { get; set; } = new MemoryStatistics();

        /// <summary>
        /// CPU usage statistics.
        /// </summary>
        public CpuStatistics CpuStats { get; set; } = new CpuStatistics();

        /// <summary>
        /// Test execution trends.
        /// </summary>
        public List<TestTrend> Trends { get; set; } = new List<TestTrend>();

        /// <summary>
        /// Performance alerts.
        /// </summary>
        public List<PerformanceAlert> Alerts { get; set; } = new List<PerformanceAlert>();

        /// <summary>
        /// Environment-specific statistics.
        /// </summary>
        public Dictionary<string, EnvironmentStatistics> EnvironmentStats { get; set; } = new Dictionary<string, EnvironmentStatistics>();

        /// <summary>
        /// Environment breakdown (alias for EnvironmentStats).
        /// </summary>
        public Dictionary<string, EnvironmentStatistics> EnvironmentBreakdown => EnvironmentStats;

        /// <summary>
        /// Project breakdown statistics.
        /// </summary>
        public Dictionary<string, ProjectStatistics> ProjectBreakdown { get; set; } = new Dictionary<string, ProjectStatistics>();
    }

    /// <summary>
    /// Memory usage statistics.
    /// </summary>
    public class MemoryStatistics
    {
        /// <summary>
        /// Current memory usage in bytes.
        /// </summary>
        public long CurrentUsageBytes { get; set; }

        /// <summary>
        /// Peak memory usage in bytes.
        /// </summary>
        public long PeakUsageBytes { get; set; }

        /// <summary>
        /// Available memory in bytes.
        /// </summary>
        public long AvailableBytes { get; set; }

        /// <summary>
        /// Memory usage percentage.
        /// </summary>
        public double UsagePercentage { get; set; }
    }

    /// <summary>
    /// CPU usage statistics.
    /// </summary>
    public class CpuStatistics
    {
        /// <summary>
        /// Current CPU usage percentage.
        /// </summary>
        public double CurrentUsagePercentage { get; set; }

        /// <summary>
        /// Average CPU usage percentage.
        /// </summary>
        public double AverageUsagePercentage { get; set; }

        /// <summary>
        /// Peak CPU usage percentage.
        /// </summary>
        public double PeakUsagePercentage { get; set; }

        /// <summary>
        /// Number of CPU cores being used.
        /// </summary>
        public int CoresInUse { get; set; }
    }

    /// <summary>
    /// Test execution trend.
    /// </summary>
    public class TestTrend
    {
        /// <summary>
        /// Trend type.
        /// </summary>
        public TrendType Type { get; set; }

        /// <summary>
        /// Trend description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Trend value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Previous value for comparison.
        /// </summary>
        public double PreviousValue { get; set; }

        /// <summary>
        /// Change percentage.
        /// </summary>
        public double ChangePercentage => PreviousValue > 0 ? ((Value - PreviousValue) / PreviousValue) * 100 : 0;
    }

    /// <summary>
    /// Types of trends.
    /// </summary>
    public enum TrendType
    {
        SuccessRate,
        ExecutionTime,
        MemoryUsage,
        CpuUsage,
        TestCount,
        FailureRate
    }

    /// <summary>
    /// Environment-specific statistics.
    /// </summary>
    public class EnvironmentStatistics
    {
        /// <summary>
        /// Environment name.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Number of active test runs in this environment.
        /// </summary>
        public int ActiveRuns { get; set; }

        /// <summary>
        /// Total tests completed in this environment.
        /// </summary>
        public int TestsCompleted { get; set; }

        /// <summary>
        /// Success rate in this environment.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Average execution time in this environment.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Project-specific statistics.
    /// </summary>
    public class ProjectStatistics
    {
        /// <summary>
        /// Project name.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Number of active test runs in this project.
        /// </summary>
        public int ActiveRuns { get; set; }

        /// <summary>
        /// Total tests completed in this project.
        /// </summary>
        public int TestsCompleted { get; set; }

        /// <summary>
        /// Success rate in this project.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Average execution time in this project.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }
    }
}