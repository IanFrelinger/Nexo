using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Shared.Values;

namespace Nexo.Shared.Models.Resource
{
    /// <summary>
    /// Resource monitoring request.
    /// </summary>
    public partial class ResourceMonitoringRequest
    {
        /// <summary>
        /// Gets or sets the resource identifier to monitor.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monitoring interval.
        /// </summary>
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the monitoring duration.
        /// </summary>
        public TimeSpan? MonitoringDuration { get; set; }

        /// <summary>
        /// Gets or sets the metrics to monitor.
        /// </summary>
        public List<string> MetricsToMonitor { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the alert thresholds.
        /// </summary>
        public Dictionary<string, double> AlertThresholds { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets additional monitoring metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource monitoring result.
    /// </summary>
    public partial class ResourceMonitoringResult
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monitoring start time.
        /// </summary>
        public DateTime MonitoringStartTime { get; set; }

        /// <summary>
        /// Gets or sets the monitoring end time.
        /// </summary>
        public DateTime? MonitoringEndTime { get; set; }

        /// <summary>
        /// Gets or sets the collected metrics.
        /// </summary>
        public Dictionary<string, object> CollectedMetrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets any alerts that were triggered.
        /// </summary>
        public List<ResourceAlert> Alerts { get; set; } = new List<ResourceAlert>();

        /// <summary>
        /// Gets or sets the monitoring status.
        /// </summary>
        public ResourceHealth Status { get; set; } = ResourceHealth.Unknown;

        /// <summary>
        /// Gets or sets additional monitoring metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource alert.
    /// </summary>
    public partial class ResourceAlert
    {
        /// <summary>
        /// Gets or sets the alert identifier.
        /// </summary>
        public string AlertId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the alert type.
        /// </summary>
        public ResourceAlertType AlertType { get; set; } = ResourceAlertType.Usage;

        /// <summary>
        /// Gets or sets the alert severity.
        /// </summary>
        public ResourceAlertSeverity Severity { get; set; } = ResourceAlertSeverity.Info;

        /// <summary>
        /// Gets or sets the alert message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the alert timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the alert metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource monitoring information.
    /// </summary>
    public partial class ResourceMonitoringInfo
    {
        /// <summary>
        /// Gets or sets the resource type being monitored.
        /// </summary>
        public ResourceType ResourceType { get; set; } = ResourceType.CPU;

        /// <summary>
        /// Gets or sets the current health status.
        /// </summary>
        public ResourceHealth Status { get; set; } = ResourceHealth.Unknown;

        /// <summary>
        /// Gets or sets the monitoring interval.
        /// </summary>
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the alert thresholds.
        /// </summary>
        public Dictionary<string, double> AlertThresholds { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets whether monitoring is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the timestamp when monitoring started.
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the monitoring.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Resource optimization result.
    /// </summary>
    public partial class ResourceOptimizationResult
    {
        /// <summary>
        /// Gets or sets whether optimization was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the optimization score.
        /// </summary>
        public double OptimizationScore { get; set; }

        /// <summary>
        /// Gets or sets the recommended actions.
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the potential savings.
        /// </summary>
        public Dictionary<string, double> PotentialSavings { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets or sets any error message if optimization failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when optimization was performed.
        /// </summary>
        public DateTime OptimizedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the optimization.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
