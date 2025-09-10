using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.Monitoring
{
    /// <summary>
    /// Interface for production monitoring service.
    /// Part of Phase 3.4 production readiness features.
    /// </summary>
    public interface IProductionMonitoringService
    {
        /// <summary>
        /// Starts comprehensive production monitoring.
        /// </summary>
        /// <param name="configuration">Monitoring configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Monitoring result.</returns>
        Task<MonitoringResult> StartMonitoringAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current monitoring status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Monitoring status.</returns>
        Task<MonitoringStatus> GetMonitoringStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitoring alerts.
        /// </summary>
        /// <param name="filter">Alert filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of monitoring alerts.</returns>
        Task<IEnumerable<MonitoringAlert>> GetAlertsAsync(
            AlertFilter filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges an alert.
        /// </summary>
        /// <param name="alertId">Alert ID.</param>
        /// <param name="acknowledgedBy">User who acknowledged the alert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if alert was acknowledged successfully.</returns>
        Task<bool> AcknowledgeAlertAsync(
            string alertId,
            string acknowledgedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves an alert.
        /// </summary>
        /// <param name="alertId">Alert ID.</param>
        /// <param name="resolvedBy">User who resolved the alert.</param>
        /// <param name="resolution">Resolution description.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if alert was resolved successfully.</returns>
        Task<bool> ResolveAlertAsync(
            string alertId,
            string resolvedBy,
            string resolution,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitoring metrics.
        /// </summary>
        /// <param name="timeWindow">Time window for metrics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Monitoring metrics.</returns>
        Task<MonitoringMetrics> GetMetricsAsync(
            TimeSpan timeWindow,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Monitoring configuration.
    /// </summary>
    public class MonitoringConfiguration
    {
        public string Name { get; set; } = "Default";
        public bool MonitorPerformance { get; set; } = true;
        public bool MonitorSecurity { get; set; } = true;
        public bool MonitorCompliance { get; set; } = true;
        public bool MonitorSystemHealth { get; set; } = true;
        public bool MonitorBusinessMetrics { get; set; } = true;
        
        public TimeSpan PerformanceCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan SecurityCheckInterval { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan ComplianceCheckInterval { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan SystemHealthCheckInterval { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan BusinessMetricsCheckInterval { get; set; } = TimeSpan.FromMinutes(30);
        
        public TimeSpan MaxMonitoringTime { get; set; } = TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Monitoring result.
    /// </summary>
    public class MonitoringResult
    {
        public MonitoringConfiguration Configuration { get; set; } = new();
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int AlertsGenerated { get; set; }
        public int CriticalAlerts { get; set; }
        public int HighAlerts { get; set; }
        public int MediumAlerts { get; set; }
        public int LowAlerts { get; set; }
    }

    /// <summary>
    /// Monitoring status.
    /// </summary>
    public class MonitoringStatus
    {
        public DateTimeOffset CheckTime { get; set; }
        public bool IsHealthy { get; set; }
        public string? ErrorMessage { get; set; }
        public PerformanceStatus? PerformanceStatus { get; set; }
        public SecurityStatus? SecurityStatus { get; set; }
        public SystemHealth? SystemHealth { get; set; }
        public List<MonitoringAlert> ActiveAlerts { get; set; } = new();
        public int AlertCount { get; set; }
    }

    /// <summary>
    /// Performance status.
    /// </summary>
    public class PerformanceStatus
    {
        public PerformanceTrend OverallTrend { get; set; }
        public PerformanceTrend CacheHitRateTrend { get; set; }
        public PerformanceTrend AIResponseTimeTrend { get; set; }
        public PerformanceTrend MemoryUsageTrend { get; set; }
    }

    /// <summary>
    /// Security status.
    /// </summary>
    public class SecurityStatus
    {
        public bool IsCompliant { get; set; }
        public double ComplianceScore { get; set; }
        public DateTimeOffset LastAuditTime { get; set; }
    }

    /// <summary>
    /// System health.
    /// </summary>
    public class SystemHealth
    {
        public DateTimeOffset CheckTime { get; set; }
        public bool IsHealthy { get; set; }
        public string? ErrorMessage { get; set; }
        public long MemoryUsageMB { get; set; }
        public bool MemoryHealthy { get; set; }
        public int ThreadCount { get; set; }
        public bool ThreadHealthy { get; set; }
        public int HandleCount { get; set; }
        public bool HandleHealthy { get; set; }
    }

    /// <summary>
    /// Monitoring alert.
    /// </summary>
    public class MonitoringAlert
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool Acknowledged { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTimeOffset? AcknowledgedAt { get; set; }
        public bool Resolved { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
    }

    /// <summary>
    /// Alert filter.
    /// </summary>
    public class AlertFilter
    {
        public AlertSeverity? Severity { get; set; }
        public string? Category { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public bool? Acknowledged { get; set; }
        public bool? Resolved { get; set; }
    }

    /// <summary>
    /// Monitoring metrics.
    /// </summary>
    public class MonitoringMetrics
    {
        public TimeSpan TimeWindow { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public PerformanceMetrics? PerformanceMetrics { get; set; }
        public SecurityMetrics? SecurityMetrics { get; set; }
        public SystemMetrics? SystemMetrics { get; set; }
        public AlertMetrics? AlertMetrics { get; set; }
    }

    /// <summary>
    /// Performance metrics.
    /// </summary>
    public class PerformanceMetrics
    {
        public PerformanceTrend OverallTrend { get; set; }
        public PerformanceTrend CacheHitRateTrend { get; set; }
        public PerformanceTrend AIResponseTimeTrend { get; set; }
        public PerformanceTrend MemoryUsageTrend { get; set; }
    }

    /// <summary>
    /// Security metrics.
    /// </summary>
    public class SecurityMetrics
    {
        public double ComplianceScore { get; set; }
        public bool IsCompliant { get; set; }
        public DateTimeOffset LastAuditTime { get; set; }
    }

    /// <summary>
    /// System metrics.
    /// </summary>
    public class SystemMetrics
    {
        public double CPUUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// Alert metrics.
    /// </summary>
    public class AlertMetrics
    {
        public int TotalAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int HighAlerts { get; set; }
        public int MediumAlerts { get; set; }
        public int LowAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
    }

    /// <summary>
    /// Alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Performance trend model.
    /// </summary>
    public class PerformanceTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public List<double> Values { get; set; } = new();
        public List<DateTimeOffset> Timestamps { get; set; } = new();
        public double CurrentValue { get; set; }
        public double AverageValue { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public TrendDirection Direction { get; set; }
        public double ChangePercentage { get; set; }
    }

    /// <summary>
    /// Trend direction enum.
    /// </summary>
    public enum TrendDirection
    {
        Up,
        Down,
        Stable
    }
}
