using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Trends analysis for test results over time.
    /// </summary>
    public class TestResultTrends
    {
        /// <summary>
        /// Start date for the trend analysis.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date for the trend analysis.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Overall success rate trend.
        /// </summary>
        public TrendData SuccessRateTrend { get; set; } = new TrendData();

        /// <summary>
        /// Execution time trend.
        /// </summary>
        public TrendData ExecutionTimeTrend { get; set; } = new TrendData();

        /// <summary>
        /// Test count trend.
        /// </summary>
        public TrendData TestCountTrend { get; set; } = new TrendData();

        /// <summary>
        /// Failure rate trend.
        /// </summary>
        public TrendData FailureRateTrend { get; set; } = new TrendData();

        /// <summary>
        /// Memory usage trend.
        /// </summary>
        public TrendData MemoryUsageTrend { get; set; } = new TrendData();

        /// <summary>
        /// CPU usage trend.
        /// </summary>
        public TrendData CpuUsageTrend { get; set; } = new TrendData();

        /// <summary>
        /// Environment-specific trends.
        /// </summary>
        public Dictionary<string, EnvironmentTrends> EnvironmentTrends { get; set; } = new Dictionary<string, EnvironmentTrends>();

        /// <summary>
        /// Project-specific trends.
        /// </summary>
        public Dictionary<string, ProjectTrends> ProjectTrends { get; set; } = new Dictionary<string, ProjectTrends>();

        /// <summary>
        /// Performance regression analysis.
        /// </summary>
        public List<PerformanceRegression> PerformanceRegressions { get; set; } = new List<PerformanceRegression>();

        /// <summary>
        /// Flaky test analysis.
        /// </summary>
        public List<FlakyTestAnalysis> FlakyTests { get; set; } = new List<FlakyTestAnalysis>();

        /// <summary>
        /// Seasonal patterns detected.
        /// </summary>
        public List<SeasonalPattern> SeasonalPatterns { get; set; } = new List<SeasonalPattern>();

        /// <summary>
        /// Recommendations based on trends.
        /// </summary>
        public List<TrendRecommendation> Recommendations { get; set; } = new List<TrendRecommendation>();

        /// <summary>
        /// Period of analysis in days.
        /// </summary>
        public int PeriodDays { get; set; }

        /// <summary>
        /// Total number of test runs analyzed.
        /// </summary>
        public int TotalRuns { get; set; }

        /// <summary>
        /// Number of successful test runs.
        /// </summary>
        public int SuccessfulRuns { get; set; }

        /// <summary>
        /// Number of failed test runs.
        /// </summary>
        public int FailedRuns { get; set; }

        /// <summary>
        /// Average success rate over the period.
        /// </summary>
        public double AverageSuccessRate { get; set; }

        /// <summary>
        /// Average execution time in milliseconds.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Daily trend data points.
        /// </summary>
        public List<DailyTrendPoint> DailyTrends { get; set; } = new List<DailyTrendPoint>();
    }

    /// <summary>
    /// Trend data for a specific metric.
    /// </summary>
    public class TrendData
    {
        /// <summary>
        /// Data points for the trend.
        /// </summary>
        public List<TrendPoint> DataPoints { get; set; } = new List<TrendPoint>();

        /// <summary>
        /// Overall trend direction.
        /// </summary>
        public TrendDirection Direction { get; set; }

        /// <summary>
        /// Trend strength (0-1).
        /// </summary>
        public double Strength { get; set; }

        /// <summary>
        /// Average value over the period.
        /// </summary>
        public double AverageValue { get; set; }

        /// <summary>
        /// Minimum value in the period.
        /// </summary>
        public double MinValue { get; set; }

        /// <summary>
        /// Maximum value in the period.
        /// </summary>
        public double MaxValue { get; set; }

        /// <summary>
        /// Standard deviation.
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// Percentage change from start to end.
        /// </summary>
        public double PercentageChange { get; set; }
    }

    /// <summary>
    /// Individual data point in a trend.
    /// </summary>
    public class TrendPoint
    {
        /// <summary>
        /// Timestamp for this data point.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Value at this timestamp.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Number of samples at this timestamp.
        /// </summary>
        public int SampleCount { get; set; }
    }

    /// <summary>
    /// Trend direction.
    /// </summary>
    public enum TrendDirection
    {
        /// <summary>
        /// Increasing trend.
        /// </summary>
        Increasing,

        /// <summary>
        /// Decreasing trend.
        /// </summary>
        Decreasing,

        /// <summary>
        /// Stable trend.
        /// </summary>
        Stable,

        /// <summary>
        /// Fluctuating trend.
        /// </summary>
        Fluctuating
    }

    /// <summary>
    /// Environment-specific trends.
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

    /// <summary>
    /// Project-specific trends.
    /// </summary>
    public class ProjectTrends
    {
        /// <summary>
        /// Project name.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Success rate trend for this project.
        /// </summary>
        public TrendData SuccessRateTrend { get; set; } = new TrendData();

        /// <summary>
        /// Test count trend for this project.
        /// </summary>
        public TrendData TestCountTrend { get; set; } = new TrendData();

        /// <summary>
        /// Coverage trend for this project.
        /// </summary>
        public TrendData CoverageTrend { get; set; } = new TrendData();
    }

    /// <summary>
    /// Performance regression analysis.
    /// </summary>
    public class PerformanceRegression
    {
        /// <summary>
        /// Metric that regressed.
        /// </summary>
        public string Metric { get; set; } = string.Empty;

        /// <summary>
        /// Regression severity.
        /// </summary>
        public RegressionSeverity Severity { get; set; }

        /// <summary>
        /// Start date of the regression.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the regression.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Percentage degradation.
        /// </summary>
        public double DegradationPercentage { get; set; }

        /// <summary>
        /// Affected tests.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();

        /// <summary>
        /// Possible causes.
        /// </summary>
        public List<string> PossibleCauses { get; set; } = new List<string>();
    }

    /// <summary>
    /// Regression severity levels.
    /// </summary>
    public enum RegressionSeverity
    {
        /// <summary>
        /// Minor regression.
        /// </summary>
        Minor,

        /// <summary>
        /// Moderate regression.
        /// </summary>
        Moderate,

        /// <summary>
        /// Major regression.
        /// </summary>
        Major,

        /// <summary>
        /// Critical regression.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Flaky test analysis.
    /// </summary>
    public class FlakyTestAnalysis
    {
        /// <summary>
        /// Test name.
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Flakiness percentage.
        /// </summary>
        public double FlakinessPercentage { get; set; }

        /// <summary>
        /// Number of runs analyzed.
        /// </summary>
        public int TotalRuns { get; set; }

        /// <summary>
        /// Number of inconsistent results.
        /// </summary>
        public int InconsistentRuns { get; set; }

        /// <summary>
        /// Common failure patterns.
        /// </summary>
        public List<string> FailurePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Suggested fixes.
        /// </summary>
        public List<string> SuggestedFixes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Seasonal pattern in test results.
    /// </summary>
    public class SeasonalPattern
    {
        /// <summary>
        /// Pattern type.
        /// </summary>
        public SeasonalPatternType Type { get; set; }

        /// <summary>
        /// Pattern description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Pattern strength (0-1).
        /// </summary>
        public double Strength { get; set; }

        /// <summary>
        /// Affected metrics.
        /// </summary>
        public List<string> AffectedMetrics { get; set; } = new List<string>();

        /// <summary>
        /// Time range for the pattern.
        /// </summary>
        public TimeRange TimeRange { get; set; } = new TimeRange();
    }

    /// <summary>
    /// Types of seasonal patterns.
    /// </summary>
    public enum SeasonalPatternType
    {
        /// <summary>
        /// Daily pattern.
        /// </summary>
        Daily,

        /// <summary>
        /// Weekly pattern.
        /// </summary>
        Weekly,

        /// <summary>
        /// Monthly pattern.
        /// </summary>
        Monthly,

        /// <summary>
        /// Quarterly pattern.
        /// </summary>
        Quarterly,

        /// <summary>
        /// Yearly pattern.
        /// </summary>
        Yearly
    }

    /// <summary>
    /// Time range for patterns.
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// Start time.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Days of week (if applicable).
        /// </summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        /// <summary>
        /// Months (if applicable).
        /// </summary>
        public List<int> Months { get; set; } = new List<int>();
    }

    /// <summary>
    /// Recommendation based on trend analysis.
    /// </summary>
    public class TrendRecommendation
    {
        /// <summary>
        /// Recommendation type.
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// Recommendation title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Recommendation description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Priority level.
        /// </summary>
        public RecommendationPriority Priority { get; set; }

        /// <summary>
        /// Estimated impact.
        /// </summary>
        public string EstimatedImpact { get; set; } = string.Empty;

        /// <summary>
        /// Suggested actions.
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Types of recommendations.
    /// </summary>
    public enum RecommendationType
    {
        /// <summary>
        /// Performance optimization.
        /// </summary>
        PerformanceOptimization,

        /// <summary>
        /// Test stability improvement.
        /// </summary>
        TestStability,

        /// <summary>
        /// Infrastructure improvement.
        /// </summary>
        Infrastructure,

        /// <summary>
        /// Process improvement.
        /// </summary>
        ProcessImprovement,

        /// <summary>
        /// Monitoring enhancement.
        /// </summary>
        MonitoringEnhancement
    }

    /// <summary>
    /// Recommendation priority levels.
    /// </summary>
    public enum RecommendationPriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Medium priority.
        /// </summary>
        Medium,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical
    }

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
        /// Success rate on this date.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Average execution time on this date.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Number of passed tests on this date.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of failed tests on this date.
        /// </summary>
        public int FailedTests { get; set; }
    }
}