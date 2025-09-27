using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Core.Application.Interfaces.Monitoring;

namespace Nexo.Infrastructure.Services.Monitoring
{
    /// <summary>
    /// Metrics collection functionality
    /// </summary>
    public partial class ProductionMonitoringService : IProductionMonitoringService
    {
        /// <summary>
        /// Gets monitoring metrics.
        /// </summary>
        public async Task<MonitoringMetrics> GetMetricsAsync(
            TimeSpan timeWindow,
            CancellationToken cancellationToken = default)
        {
            var metrics = new MonitoringMetrics
            {
                TimeWindow = timeWindow,
                StartTime = DateTimeOffset.UtcNow - timeWindow,
                EndTime = DateTimeOffset.UtcNow
            };

            try
            {
                // Get performance metrics
                var performanceTrends = await _performanceOptimizer.GetPerformanceTrendsAsync(timeWindow, cancellationToken);
                metrics.PerformanceMetrics = new PerformanceMetrics
                {
                    OverallTrend = MapPerformanceTrend(performanceTrends.OverallPerformanceTrend),
                    CacheHitRateTrend = MapPerformanceTrend(performanceTrends.CacheHitRateTrend),
                    AIResponseTimeTrend = MapPerformanceTrend(performanceTrends.AIResponseTimeTrend),
                    MemoryUsageTrend = MapPerformanceTrend(performanceTrends.MemoryUsageTrend)
                };

                // Get security metrics
                var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);
                metrics.SecurityMetrics = new Nexo.Core.Application.Interfaces.Monitoring.SecurityMetrics
                {
                    ComplianceScore = complianceStatus.OverallComplianceScore,
                    IsCompliant = complianceStatus.IsCompliant,
                    LastAuditTime = DateTimeOffset.UtcNow.AddHours(-1) // Simulated
                };

                // Get system metrics
                metrics.SystemMetrics = await GetSystemMetricsAsync(cancellationToken);

                // Get alert metrics
                lock (_lock)
                {
                    metrics.AlertMetrics = new AlertMetrics
                    {
                        TotalAlerts = _activeAlerts.Count,
                        CriticalAlerts = _activeAlerts.Values.Count(a => a.Severity == AlertSeverity.Critical),
                        HighAlerts = _activeAlerts.Values.Count(a => a.Severity == AlertSeverity.High),
                        MediumAlerts = _activeAlerts.Values.Count(a => a.Severity == AlertSeverity.Medium),
                        LowAlerts = _activeAlerts.Values.Count(a => a.Severity == AlertSeverity.Low),
                        AcknowledgedAlerts = _activeAlerts.Values.Count(a => a.Acknowledged),
                        ResolvedAlerts = _activeAlerts.Values.Count(a => a.Resolved)
                    };
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring metrics");
                return metrics;
            }
        }

        private Task<SystemHealth> GetSystemHealthAsync(CancellationToken cancellationToken)
        {
            var health = new SystemHealth
            {
                CheckTime = DateTimeOffset.UtcNow,
                IsHealthy = true
            };

            try
            {
                // Check memory usage
                var memoryUsage = GC.GetTotalMemory(false);
                health.MemoryUsageMB = memoryUsage / 1024 / 1024;
                health.MemoryHealthy = memoryUsage < 500 * 1024 * 1024; // 500MB threshold

                // Check thread count
                health.ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                health.ThreadHealthy = health.ThreadCount < 100; // 100 thread threshold

                // Check handle count
                health.HandleCount = System.Diagnostics.Process.GetCurrentProcess().HandleCount;
                health.HandleHealthy = health.HandleCount < 1000; // 1000 handle threshold

                health.IsHealthy = health.MemoryHealthy && health.ThreadHealthy && health.HandleHealthy;

                if (!health.IsHealthy)
                {
                    var issues = new List<string>();
                    if (!health.MemoryHealthy) issues.Add($"High memory usage: {health.MemoryUsageMB}MB");
                    if (!health.ThreadHealthy) issues.Add($"High thread count: {health.ThreadCount}");
                    if (!health.HandleHealthy) issues.Add($"High handle count: {health.HandleCount}");
                    health.ErrorMessage = string.Join(", ", issues);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system health");
                health.IsHealthy = false;
                health.ErrorMessage = ex.Message;
            }

            return Task.FromResult(health);
        }

        private Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken cancellationToken)
        {
            var metrics = new SystemMetrics();

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                metrics.CPUUsagePercent = process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100;
                metrics.MemoryUsageMB = process.WorkingSet64 / 1024 / 1024;
                metrics.ThreadCount = process.Threads.Count;
                metrics.HandleCount = process.HandleCount;
                metrics.Uptime = DateTimeOffset.UtcNow - process.StartTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
            }

            return Task.FromResult(metrics);
        }
    }
}
