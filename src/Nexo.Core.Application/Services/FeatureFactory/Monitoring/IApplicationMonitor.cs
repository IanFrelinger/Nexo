using Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.FeatureFactory.Monitoring;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.Monitoring
{
    /// <summary>
    /// Interface for monitoring deployed applications
    /// </summary>
    public interface IApplicationMonitor
    {
        /// <summary>
        /// Sets up health monitoring for an application
        /// </summary>
        Task<MonitoringResult> SetupHealthMonitoringAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up performance monitoring for an application
        /// </summary>
        Task<MonitoringResult> SetupPerformanceMonitoringAsync(ApplicationLogicResult applicationLogic, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up alerting for an application
        /// </summary>
        Task<MonitoringResult> SetupAlertingAsync(ApplicationLogicResult applicationLogic, AlertConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up logging for an application
        /// </summary>
        Task<MonitoringResult> SetupLoggingAsync(ApplicationLogicResult applicationLogic, LoggingConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up dashboard for an application
        /// </summary>
        Task<MonitoringResult> SetupDashboardAsync(ApplicationLogicResult applicationLogic, DashboardConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets application health status
        /// </summary>
        Task<ApplicationHealth> GetApplicationHealthAsync(string applicationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics for an application
        /// </summary>
        Task<PerformanceMetrics> GetPerformanceMetricsAsync(string applicationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitoring alerts for an application
        /// </summary>
        Task<List<MonitoringAlert>> GetAlertsAsync(string applicationId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of monitoring operations
    /// </summary>
    public class MonitoringResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string MonitoringId { get; set; } = string.Empty;
        public MonitoringType Type { get; set; } = MonitoringType.Health;
        public MonitoringStatus Status { get; set; } = new();
        public List<MonitoringConfiguration> Configurations { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monitoring status information
    /// </summary>
    public class MonitoringStatus
    {
        public string Id { get; set; } = string.Empty;
        public MonitoringState State { get; set; } = MonitoringState.Pending;
        public string Message { get; set; } = string.Empty;
        public int Progress { get; set; }
        public List<MonitoringStep> Steps { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monitoring step information
    /// </summary>
    public class MonitoringStep
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monitoring configuration
    /// </summary>
    public class MonitoringConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ConfigurationType Type { get; set; } = ConfigurationType.Health;
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monitoring alert
    /// </summary>
    public class MonitoringAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ApplicationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
        public AlertStatus Status { get; set; } = AlertStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Enums for monitoring operations

    /// <summary>
    /// Types of monitoring
    /// </summary>
    public enum MonitoringType
    {
        Health,
        Performance,
        Alerting,
        Logging,
        Dashboard
    }

    /// <summary>
    /// Monitoring states
    /// </summary>
    public enum MonitoringState
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Disabled
    }

    /// <summary>
    /// Step status
    /// </summary>
    public enum StepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>
    /// Configuration types
    /// </summary>
    public enum ConfigurationType
    {
        Health,
        Performance,
        Alerting,
        Logging,
        Dashboard
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
    /// Alert status
    /// </summary>
    public enum AlertStatus
    {
        Active,
        Acknowledged,
        Resolved,
        Suppressed
    }
}
