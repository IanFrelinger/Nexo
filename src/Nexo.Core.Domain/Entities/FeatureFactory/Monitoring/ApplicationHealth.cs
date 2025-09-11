using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.Monitoring
{
    /// <summary>
    /// Represents the health status of an application
    /// </summary>
    public class ApplicationHealth
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ApplicationId { get; set; } = string.Empty;
        public HealthStatus Status { get; set; } = HealthStatus.Healthy;
        public string Message { get; set; } = string.Empty;
        public List<HealthCheck> Checks { get; set; } = new();
        public List<HealthMetric> Metrics { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a health check
    /// </summary>
    public class HealthCheck
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HealthCheckStatus Status { get; set; } = HealthCheckStatus.Healthy;
        public string Message { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a health metric
    /// </summary>
    public class HealthMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public MetricType Type { get; set; } = MetricType.Counter;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents performance metrics for an application
    /// </summary>
    public class PerformanceMetrics
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ApplicationId { get; set; } = string.Empty;
        public List<PerformanceMetric> Metrics { get; set; } = new();
        public List<PerformanceCounter> Counters { get; set; } = new();
        public List<PerformanceGauge> Gauges { get; set; } = new();
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a performance metric
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public MetricType Type { get; set; } = MetricType.Counter;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a performance counter
    /// </summary>
    public class PerformanceCounter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public CounterType Type { get; set; } = CounterType.Increment;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a performance gauge
    /// </summary>
    public class PerformanceGauge
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public GaugeType Type { get; set; } = GaugeType.Value;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an alert configuration
    /// </summary>
    public class AlertConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AlertType Type { get; set; } = AlertType.Threshold;
        public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
        public List<AlertCondition> Conditions { get; set; } = new();
        public List<AlertAction> Actions { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an alert condition
    /// </summary>
    public class AlertCondition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an alert action
    /// </summary>
    public class AlertAction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ActionType Type { get; set; } = ActionType.Email;
        public string Target { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a logging configuration
    /// </summary>
    public class LoggingConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LogLevel Level { get; set; } = LogLevel.Information;
        public List<LogTarget> Targets { get; set; } = new();
        public List<LogFilter> Filters { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a log target
    /// </summary>
    public class LogTarget
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TargetType Type { get; set; } = TargetType.Console;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a log filter
    /// </summary>
    public class LogFilter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public FilterType Type { get; set; } = FilterType.Include;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a dashboard configuration
    /// </summary>
    public class DashboardConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DashboardWidget> Widgets { get; set; } = new();
        public List<DashboardLayout> Layouts { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a dashboard widget
    /// </summary>
    public class DashboardWidget
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WidgetType Type { get; set; } = WidgetType.Chart;
        public string DataSource { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a dashboard layout
    /// </summary>
    public class DashboardLayout
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<LayoutItem> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a layout item
    /// </summary>
    public class LayoutItem
    {
        public string WidgetName { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Enums for monitoring

    /// <summary>
    /// Health status levels
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Health check status
    /// </summary>
    public enum HealthCheckStatus
    {
        Healthy,
        Unhealthy,
        Degraded,
        Unknown
    }

    /// <summary>
    /// Metric types
    /// </summary>
    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Summary
    }

    /// <summary>
    /// Counter types
    /// </summary>
    public enum CounterType
    {
        Increment,
        Decrement,
        Reset
    }

    /// <summary>
    /// Gauge types
    /// </summary>
    public enum GaugeType
    {
        Value,
        Rate,
        Percentage
    }

    /// <summary>
    /// Alert types
    /// </summary>
    public enum AlertType
    {
        Threshold,
        Anomaly,
        Pattern,
        Custom
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Action types
    /// </summary>
    public enum ActionType
    {
        Email,
        SMS,
        Webhook,
        Slack,
        Teams,
        PagerDuty
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Target types
    /// </summary>
    public enum TargetType
    {
        Console,
        File,
        Database,
        Cloud,
        Custom
    }

    /// <summary>
    /// Filter types
    /// </summary>
    public enum FilterType
    {
        Include,
        Exclude
    }

    /// <summary>
    /// Widget types
    /// </summary>
    public enum WidgetType
    {
        Chart,
        Table,
        Gauge,
        Text,
        Map,
        Custom
    }
}
