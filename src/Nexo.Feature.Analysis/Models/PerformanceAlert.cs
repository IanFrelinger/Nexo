using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Performance alert for test execution.
    /// </summary>
    public class PerformanceAlert
    {
        /// <summary>
        /// Unique identifier for this alert.
        /// </summary>
        public string AlertId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Alert type.
        /// </summary>
        public AlertType Type { get; set; }

        /// <summary>
        /// Alert severity level.
        /// </summary>
        public AlertSeverity Severity { get; set; }

        /// <summary>
        /// Alert title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Alert description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the alert was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Environment where the alert occurred.
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Project where the alert occurred.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Test run ID associated with this alert.
        /// </summary>
        public string RunId { get; set; } = string.Empty;

        /// <summary>
        /// Current value that triggered the alert.
        /// </summary>
        public double CurrentValue { get; set; }

        /// <summary>
        /// Threshold value that was exceeded.
        /// </summary>
        public double ThresholdValue { get; set; }

        /// <summary>
        /// Unit of measurement for the values.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Whether the alert has been acknowledged.
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// Timestamp when the alert was acknowledged.
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>
        /// User who acknowledged the alert.
        /// </summary>
        public string AcknowledgedBy { get; set; } = string.Empty;

        /// <summary>
        /// Suggested actions to resolve the alert.
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();

        /// <summary>
        /// Additional metadata for the alert.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Acknowledges the alert.
        /// </summary>
        /// <param name="acknowledgedBy">User acknowledging the alert.</param>
        public void Acknowledge(string acknowledgedBy)
        {
            IsAcknowledged = true;
            AcknowledgedAt = DateTime.UtcNow;
            AcknowledgedBy = acknowledgedBy;
        }

        /// <summary>
        /// Gets a formatted message for the alert.
        /// </summary>
        /// <returns>Formatted alert message.</returns>
        public string GetFormattedMessage()
        {
            return $"[{Severity}] {Title}: {Description} (Current: {CurrentValue:F2}{Unit}, Threshold: {ThresholdValue:F2}{Unit})";
        }
    }

    /// <summary>
    /// Types of performance alerts.
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// High memory usage alert.
        /// </summary>
        HighMemoryUsage,

        /// <summary>
        /// High CPU usage alert.
        /// </summary>
        HighCpuUsage,

        /// <summary>
        /// Slow test execution alert.
        /// </summary>
        SlowTestExecution,

        /// <summary>
        /// High failure rate alert.
        /// </summary>
        HighFailureRate,

        /// <summary>
        /// Low success rate alert.
        /// </summary>
        LowSuccessRate,

        /// <summary>
        /// Test timeout alert.
        /// </summary>
        TestTimeout,

        /// <summary>
        /// Resource exhaustion alert.
        /// </summary>
        ResourceExhaustion,

        /// <summary>
        /// Performance regression alert.
        /// </summary>
        PerformanceRegression,

        /// <summary>
        /// Custom alert.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Severity levels for alerts.
    /// </summary>
    public enum AlertSeverity
    {
        /// <summary>
        /// Informational alert.
        /// </summary>
        Info,

        /// <summary>
        /// Warning alert.
        /// </summary>
        Warning,

        /// <summary>
        /// Error alert.
        /// </summary>
        Error,

        /// <summary>
        /// Critical alert.
        /// </summary>
        Critical
    }
}