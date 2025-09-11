using Nexo.Core.Domain.Enums.Monitoring;

namespace Nexo.Core.Domain.Entities.Monitoring
{
    /// <summary>
    /// Represents a monitoring metric
    /// </summary>
    public class Metric
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public string Unit { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a health check
    /// </summary>
    public class HealthCheck
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public HealthStatus Status { get; set; }
        public string Message { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Result of health checks
    /// </summary>
    public class HealthCheckResult
    {
        public HealthStatus OverallHealth { get; set; }
        public List<HealthCheck> HealthChecks { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents an alert
    /// </summary>
    public class Alert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AlertStatus Status { get; set; } = AlertStatus.New;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Configuration for monitoring system
    /// </summary>
    public class MonitoringConfiguration
    {
        public MetricsConfiguration MetricsConfig { get; set; } = new();
        public AlertingConfiguration AlertingConfig { get; set; } = new();
        public AnalyticsConfiguration AnalyticsConfig { get; set; } = new();
        public HealthCheckConfiguration HealthCheckConfig { get; set; } = new();
    }

    /// <summary>
    /// Configuration for metrics collection
    /// </summary>
    public class MetricsConfiguration
    {
        public int CollectionIntervalSeconds { get; set; } = 30;
        public int RetentionDays { get; set; } = 30;
        public List<string> EnabledMetrics { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Configuration for alerting
    /// </summary>
    public class AlertingConfiguration
    {
        public List<AlertRule> Rules { get; set; } = new();
        public List<AlertChannel> Channels { get; set; } = new();
        public EscalationPolicy Escalation { get; set; } = new();
    }

    /// <summary>
    /// Alert rule definition
    /// </summary>
    public class AlertRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string MetricName { get; set; } = "";
        public string Condition { get; set; } = "";
        public double Threshold { get; set; }
        public AlertSeverity Severity { get; set; }
        public bool Enabled { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Alert channel configuration
    /// </summary>
    public class AlertChannel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public AlertChannelType Type { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Escalation policy for alerts
    /// </summary>
    public class EscalationPolicy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public List<EscalationLevel> Levels { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Escalation level
    /// </summary>
    public class EscalationLevel
    {
        public int Level { get; set; }
        public TimeSpan Delay { get; set; }
        public List<string> Recipients { get; set; } = new();
        public List<string> Channels { get; set; } = new();
    }

    /// <summary>
    /// Configuration for analytics
    /// </summary>
    public class AnalyticsConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int RetentionDays { get; set; } = 90;
        public List<string> TrackedEvents { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Configuration for health checks
    /// </summary>
    public class HealthCheckConfiguration
    {
        public int CheckIntervalSeconds { get; set; } = 300;
        public int TimeoutSeconds { get; set; } = 30;
        public List<string> EnabledChecks { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Result of monitoring initialization
    /// </summary>
    public class MonitoringInitializationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime InitializedAt { get; set; } = DateTime.UtcNow;
        public MonitoringConfiguration? Configuration { get; set; }
    }

    /// <summary>
    /// Result of metrics collection
    /// </summary>
    public class MetricsCollectionResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int MetricsCollected { get; set; }
        public DateTime CollectionTime { get; set; } = DateTime.UtcNow;
        public List<Metric> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Result of alerting configuration
    /// </summary>
    public class AlertingConfigurationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request for monitoring report
    /// </summary>
    public class MonitoringReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Metrics { get; set; } = new();
        public bool IncludeHealthChecks { get; set; } = true;
        public bool IncludeAnalytics { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Comprehensive monitoring report
    /// </summary>
    public class MonitoringReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; }
        public DateRange Period { get; set; } = new();
        public List<Metric> Metrics { get; set; } = new();
        public HealthCheckResult HealthChecks { get; set; } = new();
        public AnalyticsData Analytics { get; set; } = new();
        public List<Insight> Insights { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public MonitoringSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Date range for reports
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Analytics data
    /// </summary>
    public class AnalyticsData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Data { get; set; } = new();
        public int TotalEvents { get; set; }
        public int UniqueUsers { get; set; }
        public double AverageSessionDuration { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Insight from monitoring data
    /// </summary>
    public class Insight
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public InsightType Type { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public InsightSeverity Severity { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Recommendation based on monitoring data
    /// </summary>
    public class Recommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> ActionItems { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Summary of monitoring data
    /// </summary>
    public class MonitoringSummary
    {
        public HealthStatus OverallHealth { get; set; }
        public int TotalMetrics { get; set; }
        public int CriticalAlerts { get; set; }
        public double SystemUptime { get; set; }
        public double PerformanceScore { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
