using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Analytics
{
    /// <summary>
    /// Interface for comprehensive reporting service.
    /// Part of Phase 3.3 analytics and reporting features.
    /// </summary>
    public interface IComprehensiveReportingService
    {
        /// <summary>
        /// Generates a comprehensive analytics report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="configuration">Report configuration options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Comprehensive analytics report.</returns>
        Task<ComprehensiveReport> GenerateComprehensiveReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            ReportConfiguration? configuration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a usage analytics report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Usage analytics report.</returns>
        Task<UsageReport> GenerateUsageReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a performance analytics report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Performance analytics report.</returns>
        Task<PerformanceReport> GeneratePerformanceReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a security analytics report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Security analytics report.</returns>
        Task<SecurityReport> GenerateSecurityReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a cost analytics report.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Cost analytics report.</returns>
        Task<CostReport> GenerateCostReportAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports a report in the specified format.
        /// </summary>
        /// <param name="report">Report to export.</param>
        /// <param name="format">Export format.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Exported report data.</returns>
        Task<ReportExport> ExportReportAsync(
            ComprehensiveReport report,
            ReportExportFormat format,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Comprehensive report model.
    /// </summary>
    public class ComprehensiveReport
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public ReportConfiguration Configuration { get; set; } = new ReportConfiguration();
        public ExecutiveSummary ExecutiveSummary { get; set; } = new ExecutiveSummary();
        public UsageReport UsageReport { get; set; } = new UsageReport();
        public PerformanceReport PerformanceReport { get; set; } = new PerformanceReport();
        public SecurityReport SecurityReport { get; set; } = new SecurityReport();
        public CostReport CostReport { get; set; } = new CostReport();
        public List<AnalyticsRecommendation> Recommendations { get; set; } = new List<AnalyticsRecommendation>();
        public List<AnalyticsInsight> Insights { get; set; } = new List<AnalyticsInsight>();
        public RealTimeAnalytics RealTimeMetrics { get; set; } = new RealTimeAnalytics();
    }

    /// <summary>
    /// Report configuration model.
    /// </summary>
    public class ReportConfiguration
    {
        public bool IncludeUsageCharts { get; set; } = true;
        public bool IncludePerformanceCharts { get; set; } = true;
        public bool IncludeCostCharts { get; set; } = true;
        public bool IncludeRawData { get; set; } = false;
        public bool IncludeMethodology { get; set; } = true;
        public bool IncludeGlossary { get; set; } = true;
    }

    /// <summary>
    /// Executive summary model.
    /// </summary>
    public class ExecutiveSummary
    {
        public int TotalEvents { get; set; }
        public int UniqueUsers { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public decimal TotalCost { get; set; }
        public int SystemHealth { get; set; }
        public List<AnalyticsInsight> KeyInsights { get; set; } = new List<AnalyticsInsight>();
        public List<AnalyticsRecommendation> TopRecommendations { get; set; } = new List<AnalyticsRecommendation>();
    }

    /// <summary>
    /// Usage report model.
    /// </summary>
    public class UsageReport
    {
        public int TotalEvents { get; set; }
        public int UniqueUsers { get; set; }
        public int TotalTokens { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EventsByModel { get; set; } = new Dictionary<string, int>();
        public List<string> TopUsers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Performance report model.
    /// </summary>
    public class PerformanceReport
    {
        public int TotalMetrics { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public double AverageThroughput { get; set; }
        public double AverageAccuracy { get; set; }
        public double ErrorRate { get; set; }
        public ResourceUtilization ResourceUtilization { get; set; } = new ResourceUtilization();
        public List<PerformanceTrend> PerformanceTrends { get; set; } = new List<PerformanceTrend>();
        public List<PerformanceBottleneck> Bottlenecks { get; set; } = new List<PerformanceBottleneck>();
    }

    /// <summary>
    /// Security report model.
    /// </summary>
    public class SecurityReport
    {
        public int TotalEvents { get; set; }
        public int FailedAttempts { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int SecurityScore { get; set; }
    }

    /// <summary>
    /// Cost report model.
    /// </summary>
    public class CostReport
    {
        public decimal TotalCost { get; set; }
        public int TotalTokens { get; set; }
        public decimal CostPerToken { get; set; }
    }

    /// <summary>
    /// Report export model.
    /// </summary>
    public class ReportExport
    {
        public ReportExportFormat Format { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public ComprehensiveReport Report { get; set; } = new ComprehensiveReport();
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report export formats.
    /// </summary>
    public enum ReportExportFormat
    {
        Json,
        Csv,
        Xml,
        Pdf,
        Html
    }
}