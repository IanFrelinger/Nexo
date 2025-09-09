using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Analytics
{
    /// <summary>
    /// Interface for AI analytics service.
    /// Part of Phase 3.3 analytics and reporting capabilities.
    /// </summary>
    public interface IAIAnalyticsService
    {
        /// <summary>
        /// Records an AI usage event.
        /// </summary>
        /// <param name="usageEvent">The usage event to record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RecordUsageEventAsync(AIUsageEvent usageEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a performance metric.
        /// </summary>
        /// <param name="metric">The performance metric to record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RecordPerformanceMetricAsync(AIPerformanceMetric metric, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets usage analytics for a specific time period.
        /// </summary>
        /// <param name="startTime">Start time for analytics.</param>
        /// <param name="endTime">End time for analytics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Usage analytics data.</returns>
        Task<AIUsageAnalytics> GetUsageAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance analytics for a specific time period.
        /// </summary>
        /// <param name="startTime">Start time for analytics.</param>
        /// <param name="endTime">End time for analytics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Performance analytics data.</returns>
        Task<AIPerformanceAnalytics> GetPerformanceAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive analytics combining usage and performance data.
        /// </summary>
        /// <param name="startTime">Start time for analytics.</param>
        /// <param name="endTime">End time for analytics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Comprehensive analytics data.</returns>
        Task<ComprehensiveAnalytics> GetComprehensiveAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets real-time analytics for the current session.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Real-time analytics data.</returns>
        Task<RealTimeAnalytics> GetRealTimeAnalyticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports analytics data in various formats.
        /// </summary>
        /// <param name="startTime">Start time for export.</param>
        /// <param name="endTime">End time for export.</param>
        /// <param name="format">Export format.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Exported analytics data.</returns>
        Task<AnalyticsExport> ExportAnalyticsAsync(
            DateTimeOffset startTime, 
            DateTimeOffset endTime, 
            AnalyticsExportFormat format,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// AI usage event model.
    /// </summary>
    public class AIUsageEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public decimal Cost { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// AI performance metric model.
    /// </summary>
    public class AIPerformanceMetric
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string ModelName { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public TimeSpan? Latency { get; set; }
        public double? Throughput { get; set; }
        public double? Accuracy { get; set; }
        public bool IsError { get; set; }
        public double? CpuUsage { get; set; }
        public double? MemoryUsage { get; set; }
        public double? NetworkUsage { get; set; }
        public double? StorageUsage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// AI usage analytics model.
    /// </summary>
    public class AIUsageAnalytics
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int TotalEvents { get; set; }
        public int UniqueUsers { get; set; }
        public int TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EventsByModel { get; set; } = new Dictionary<string, int>();
        public List<string> TopUsers { get; set; } = new List<string>();
        public Dictionary<int, int> HourlyDistribution { get; set; } = new Dictionary<int, int>();
        public Dictionary<DateTime, int> DailyDistribution { get; set; } = new Dictionary<DateTime, int>();
    }

    /// <summary>
    /// AI performance analytics model.
    /// </summary>
    public class AIPerformanceAnalytics
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
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
    /// Comprehensive analytics model.
    /// </summary>
    public class ComprehensiveAnalytics
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public AIUsageAnalytics UsageAnalytics { get; set; } = new AIUsageAnalytics();
        public AIPerformanceAnalytics PerformanceAnalytics { get; set; } = new AIPerformanceAnalytics();
        public List<AnalyticsInsight> Insights { get; set; } = new List<AnalyticsInsight>();
        public List<AnalyticsRecommendation> Recommendations { get; set; } = new List<AnalyticsRecommendation>();
    }

    /// <summary>
    /// Real-time analytics model.
    /// </summary>
    public class RealTimeAnalytics
    {
        public DateTimeOffset Timestamp { get; set; }
        public int EventsLastHour { get; set; }
        public int ActiveUsers { get; set; }
        public double CurrentThroughput { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public double ErrorRate { get; set; }
        public SystemHealth SystemHealth { get; set; } = new SystemHealth();
    }

    /// <summary>
    /// Resource utilization model.
    /// </summary>
    public class ResourceUtilization
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double NetworkUsage { get; set; }
        public double StorageUsage { get; set; }
    }

    /// <summary>
    /// Performance trend model.
    /// </summary>
    public class PerformanceTrend
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan Latency { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Performance bottleneck model.
    /// </summary>
    public class PerformanceBottleneck
    {
        public BottleneckType Type { get; set; }
        public BottleneckSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public int AffectedOperations { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Analytics insight model.
    /// </summary>
    public class AnalyticsInsight
    {
        public InsightType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InsightImpact Impact { get; set; }
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Analytics recommendation model.
    /// </summary>
    public class AnalyticsRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>
    /// System health model.
    /// </summary>
    public class SystemHealth
    {
        public int Score { get; set; }
        public HealthStatus Status { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }

    /// <summary>
    /// Analytics export model.
    /// </summary>
    public class AnalyticsExport
    {
        public AnalyticsExportFormat Format { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }

    /// <summary>
    /// Bottleneck types.
    /// </summary>
    public enum BottleneckType
    {
        HighLatency,
        HighErrorRate,
        ResourceExhaustion,
        NetworkCongestion,
        DatabaseSlowdown
    }

    /// <summary>
    /// Bottleneck severity levels.
    /// </summary>
    public enum BottleneckSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Insight types.
    /// </summary>
    public enum InsightType
    {
        Usage,
        Performance,
        Reliability,
        Cost,
        Security
    }

    /// <summary>
    /// Insight impact levels.
    /// </summary>
    public enum InsightImpact
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Recommendation types.
    /// </summary>
    public enum RecommendationType
    {
        Performance,
        Reliability,
        Cost,
        Security,
        Scalability
    }

    /// <summary>
    /// Recommendation priority levels.
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Health status levels.
    /// </summary>
    public enum HealthStatus
    {
        Poor,
        Fair,
        Good,
        Excellent
    }

    /// <summary>
    /// Analytics export formats.
    /// </summary>
    public enum AnalyticsExportFormat
    {
        Json,
        Csv,
        Xml,
        Pdf,
        Html
    }
}