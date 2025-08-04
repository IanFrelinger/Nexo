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
}