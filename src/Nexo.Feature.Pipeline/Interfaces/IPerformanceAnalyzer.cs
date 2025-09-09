using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for analyzing pipeline performance and generating insights.
    /// </summary>
    public interface IPerformanceAnalyzer
    {
        /// <summary>
        /// Analyzes the performance of a pipeline execution result.
        /// </summary>
        /// <param name="result">The execution result to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Performance analysis results.</returns>
        Task<PerformanceAnalysis> AnalyzeAsync(
            PipelineExecutionResult result,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current system performance metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current system performance metrics.</returns>
        Task<Dictionary<string, object>> GetSystemMetricsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes performance trends over time.
        /// </summary>
        /// <param name="timeRange">The time range to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Performance trend analysis.</returns>
        Task<PerformanceTrendAnalysis> AnalyzeTrendsAsync(
            TimeRange timeRange,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Performance analysis results.
    /// </summary>
    public class PerformanceAnalysis
    {
        /// <summary>
        /// Gets or sets the analysis identifier.
        /// </summary>
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the execution ID this analysis is for.
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the overall performance score (0-100).
        /// </summary>
        public double OverallScore { get; set; }

        /// <summary>
        /// Gets or sets the performance bottlenecks identified.
        /// </summary>
        public List<PerformanceBottleneck> Bottlenecks { get; set; } = new();

        /// <summary>
        /// Gets or sets the performance strengths identified.
        /// </summary>
        public List<PerformanceStrength> Strengths { get; set; } = new();

        /// <summary>
        /// Gets or sets the optimization opportunities.
        /// </summary>
        public List<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new();

        /// <summary>
        /// Gets or sets the analysis timestamp.
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance bottleneck identified during analysis.
    /// </summary>
    public class PerformanceBottleneck
    {
        /// <summary>
        /// Gets or sets the bottleneck identifier.
        /// </summary>
        public string BottleneckId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the bottleneck type.
        /// </summary>
        public BottleneckType Type { get; set; }

        /// <summary>
        /// Gets or sets the bottleneck description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity of the bottleneck.
        /// </summary>
        public BottleneckSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the impact percentage (0-100).
        /// </summary>
        public double ImpactPercentage { get; set; }

        /// <summary>
        /// Gets or sets the affected component or behavior.
        /// </summary>
        public string AffectedComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of performance bottlenecks.
    /// </summary>
    public enum BottleneckType
    {
        /// <summary>
        /// CPU bottleneck.
        /// </summary>
        Cpu,

        /// <summary>
        /// Memory bottleneck.
        /// </summary>
        Memory,

        /// <summary>
        /// Disk I/O bottleneck.
        /// </summary>
        DiskIo,

        /// <summary>
        /// Network bottleneck.
        /// </summary>
        Network,

        /// <summary>
        /// Synchronization bottleneck.
        /// </summary>
        Synchronization,

        /// <summary>
        /// Algorithmic bottleneck.
        /// </summary>
        Algorithmic
    }

    /// <summary>
    /// Bottleneck severity levels.
    /// </summary>
    public enum BottleneckSeverity
    {
        /// <summary>
        /// Minor bottleneck.
        /// </summary>
        Minor,

        /// <summary>
        /// Moderate bottleneck.
        /// </summary>
        Moderate,

        /// <summary>
        /// Major bottleneck.
        /// </summary>
        Major,

        /// <summary>
        /// Critical bottleneck.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Performance strength identified during analysis.
    /// </summary>
    public class PerformanceStrength
    {
        /// <summary>
        /// Gets or sets the strength identifier.
        /// </summary>
        public string StrengthId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the strength type.
        /// </summary>
        public StrengthType Type { get; set; }

        /// <summary>
        /// Gets or sets the strength description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the benefit percentage (0-100).
        /// </summary>
        public double BenefitPercentage { get; set; }

        /// <summary>
        /// Gets or sets the component or behavior that provides this strength.
        /// </summary>
        public string SourceComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of performance strengths.
    /// </summary>
    public enum StrengthType
    {
        /// <summary>
        /// Efficient CPU usage.
        /// </summary>
        CpuEfficiency,

        /// <summary>
        /// Efficient memory usage.
        /// </summary>
        MemoryEfficiency,

        /// <summary>
        /// Good parallelization.
        /// </summary>
        Parallelization,

        /// <summary>
        /// Effective caching.
        /// </summary>
        Caching,

        /// <summary>
        /// Optimized algorithms.
        /// </summary>
        AlgorithmOptimization,

        /// <summary>
        /// Resource management.
        /// </summary>
        ResourceManagement
    }

    /// <summary>
    /// Optimization opportunity identified during analysis.
    /// </summary>
    public class OptimizationOpportunity
    {
        /// <summary>
        /// Gets or sets the opportunity identifier.
        /// </summary>
        public string OpportunityId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the opportunity type.
        /// </summary>
        public OptimizationType Type { get; set; }

        /// <summary>
        /// Gets or sets the opportunity description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the potential improvement percentage (0-100).
        /// </summary>
        public double PotentialImprovementPercentage { get; set; }

        /// <summary>
        /// Gets or sets the implementation complexity.
        /// </summary>
        public ImplementationComplexity ImplementationComplexity { get; set; }

        /// <summary>
        /// Gets or sets the target component or behavior.
        /// </summary>
        public string TargetComponent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance trend analysis over time.
    /// </summary>
    public class PerformanceTrendAnalysis
    {
        /// <summary>
        /// Gets or sets the analysis identifier.
        /// </summary>
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the time range analyzed.
        /// </summary>
        public TimeRange TimeRange { get; set; } = new();

        /// <summary>
        /// Gets or sets the performance trends.
        /// </summary>
        public List<Dictionary<string, object>> Trends { get; set; } = new();

        /// <summary>
        /// Gets or sets the trend direction.
        /// </summary>
        public TrendDirection TrendDirection { get; set; }

        /// <summary>
        /// Gets or sets the trend confidence level (0-100).
        /// </summary>
        public double TrendConfidence { get; set; }

        /// <summary>
        /// Gets or sets the analysis timestamp.
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Time range for analysis.
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the duration of the time range.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// Trend direction indicators.
    /// </summary>
    public enum TrendDirection
    {
        /// <summary>
        /// Performance is improving.
        /// </summary>
        Improving,

        /// <summary>
        /// Performance is stable.
        /// </summary>
        Stable,

        /// <summary>
        /// Performance is declining.
        /// </summary>
        Declining,

        /// <summary>
        /// Trend is unclear or mixed.
        /// </summary>
        Unclear
    }
}
