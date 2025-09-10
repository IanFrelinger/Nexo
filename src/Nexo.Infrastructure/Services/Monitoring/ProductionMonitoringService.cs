using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Performance;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Core.Application.Interfaces.Caching;
using Nexo.Core.Application.Interfaces.Monitoring;

namespace Nexo.Infrastructure.Services.Monitoring
{
    /// <summary>
    /// Production monitoring service for Phase 3.4.
    /// Provides comprehensive production deployment monitoring and alerting systems.
    /// </summary>
    public class ProductionMonitoringService : IProductionMonitoringService
    {
        private readonly ILogger<ProductionMonitoringService> _logger;
        private readonly IProductionPerformanceOptimizer _performanceOptimizer;
        private readonly IProductionSecurityAuditor _securityAuditor;
        private readonly ICachePerformanceMonitor _cacheMonitor;
        private readonly Dictionary<string, MonitoringAlert> _activeAlerts;
        private readonly object _lock = new object();

        public ProductionMonitoringService(
            ILogger<ProductionMonitoringService> logger,
            IProductionPerformanceOptimizer performanceOptimizer,
            IProductionSecurityAuditor securityAuditor,
            ICachePerformanceMonitor cacheMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));
            _securityAuditor = securityAuditor ?? throw new ArgumentNullException(nameof(securityAuditor));
            _cacheMonitor = cacheMonitor ?? throw new ArgumentNullException(nameof(cacheMonitor));
            _activeAlerts = new Dictionary<string, MonitoringAlert>();
        }

        /// <summary>
        /// Starts comprehensive production monitoring.
        /// </summary>
        public async Task<MonitoringResult> StartMonitoringAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting production monitoring with configuration: {Configuration}", configuration.Name);

            var result = new MonitoringResult
            {
                Configuration = configuration,
                StartTime = DateTimeOffset.UtcNow,
                Success = true
            };

            try
            {
                // Start monitoring tasks
                var tasks = new List<Task>();

                if (configuration.MonitorPerformance)
                {
                    tasks.Add(MonitorPerformanceAsync(configuration, cancellationToken));
                }

                if (configuration.MonitorSecurity)
                {
                    tasks.Add(MonitorSecurityAsync(configuration, cancellationToken));
                }

                if (configuration.MonitorCompliance)
                {
                    tasks.Add(MonitorComplianceAsync(configuration, cancellationToken));
                }

                if (configuration.MonitorSystemHealth)
                {
                    tasks.Add(MonitorSystemHealthAsync(configuration, cancellationToken));
                }

                if (configuration.MonitorBusinessMetrics)
                {
                    tasks.Add(MonitorBusinessMetricsAsync(configuration, cancellationToken));
                }

                // Wait for all monitoring tasks to complete or be cancelled
                await Task.WhenAll(tasks);

                result.EndTime = DateTimeOffset.UtcNow;
                result.Success = true;

                _logger.LogInformation("Production monitoring completed successfully in {Duration}ms", 
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Production monitoring was cancelled");
                result.Success = false;
                result.ErrorMessage = "Monitoring was cancelled";
                result.EndTime = DateTimeOffset.UtcNow;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during production monitoring");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTimeOffset.UtcNow;
                return result;
            }
        }

        /// <summary>
        /// Gets current monitoring status.
        /// </summary>
        public async Task<MonitoringStatus> GetMonitoringStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new MonitoringStatus
            {
                CheckTime = DateTimeOffset.UtcNow,
                IsHealthy = true
            };

            try
            {
                // Check performance status
                var performanceTrends = await _performanceOptimizer.GetPerformanceTrendsAsync(
                    TimeSpan.FromHours(1), cancellationToken);
                
                status.PerformanceStatus = new PerformanceStatus
                {
                    OverallTrend = MapPerformanceTrend(performanceTrends.OverallPerformanceTrend),
                    CacheHitRateTrend = MapPerformanceTrend(performanceTrends.CacheHitRateTrend),
                    AIResponseTimeTrend = MapPerformanceTrend(performanceTrends.AIResponseTimeTrend),
                    MemoryUsageTrend = MapPerformanceTrend(performanceTrends.MemoryUsageTrend)
                };

                // Check security status
                var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);
                
                status.SecurityStatus = new SecurityStatus
                {
                    IsCompliant = complianceStatus.IsCompliant,
                    ComplianceScore = complianceStatus.OverallComplianceScore,
                    LastAuditTime = DateTimeOffset.UtcNow.AddHours(-1) // Simulated
                };

                // Check system health
                status.SystemHealth = await GetSystemHealthAsync(cancellationToken);

                // Check active alerts
                lock (_lock)
                {
                    status.ActiveAlerts = _activeAlerts.Values.ToList();
                    status.AlertCount = _activeAlerts.Count;
                }

                // Determine overall health
                status.IsHealthy = status.PerformanceStatus.OverallTrend.Direction != TrendDirection.Down &&
                                 status.SecurityStatus.IsCompliant &&
                                 status.SystemHealth.IsHealthy &&
                                 status.AlertCount == 0;

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring status");
                status.IsHealthy = false;
                status.ErrorMessage = ex.Message;
                return status;
            }
        }

        /// <summary>
        /// Gets monitoring alerts.
        /// </summary>
        public Task<IEnumerable<MonitoringAlert>> GetAlertsAsync(
            AlertFilter filter,
            CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    var alerts = _activeAlerts.Values.AsEnumerable();

                    if (filter.Severity.HasValue)
                    {
                        alerts = alerts.Where(a => a.Severity == filter.Severity.Value);
                    }

                    if (filter.Category != null)
                    {
                        alerts = alerts.Where(a => a.Category == filter.Category);
                    }

                    if (filter.StartTime.HasValue)
                    {
                        alerts = alerts.Where(a => a.Timestamp >= filter.StartTime.Value);
                    }

                    if (filter.EndTime.HasValue)
                    {
                        alerts = alerts.Where(a => a.Timestamp <= filter.EndTime.Value);
                    }

                    return Task.FromResult<IEnumerable<MonitoringAlert>>(alerts.OrderByDescending(a => a.Timestamp).ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring alerts");
                return Task.FromResult<IEnumerable<MonitoringAlert>>(new List<MonitoringAlert>());
            }
        }

        /// <summary>
        /// Acknowledges an alert.
        /// </summary>
        public Task<bool> AcknowledgeAlertAsync(
            string alertId,
            string acknowledgedBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    if (_activeAlerts.TryGetValue(alertId, out var alert))
                    {
                        alert.Acknowledged = true;
                        alert.AcknowledgedBy = acknowledgedBy;
                        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
                        
                        _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, acknowledgedBy);
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Resolves an alert.
        /// </summary>
        public Task<bool> ResolveAlertAsync(
            string alertId,
            string resolvedBy,
            string resolution,
            CancellationToken cancellationToken = default)
        {
            try
            {
                lock (_lock)
                {
                    if (_activeAlerts.TryGetValue(alertId, out var alert))
                    {
                        alert.Resolved = true;
                        alert.ResolvedBy = resolvedBy;
                        alert.ResolvedAt = DateTimeOffset.UtcNow;
                        alert.Resolution = resolution;
                        
                        _logger.LogInformation("Alert {AlertId} resolved by {User}: {Resolution}", 
                            alertId, resolvedBy, resolution);
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert {AlertId}", alertId);
                return Task.FromResult(false);
            }
        }

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

        #region Private Methods

        private Nexo.Core.Application.Interfaces.Monitoring.PerformanceTrend MapPerformanceTrend(Nexo.Core.Application.Interfaces.Performance.PerformanceTrend trend)
        {
            return trend switch
            {
                Nexo.Core.Application.Interfaces.Performance.PerformanceTrend.Improving => new Nexo.Core.Application.Interfaces.Monitoring.PerformanceTrend
                {
                    MetricName = "Overall",
                    Direction = TrendDirection.Up,
                    CurrentValue = 1.0,
                    AverageValue = 0.8,
                    MinValue = 0.0,
                    MaxValue = 1.0,
                    ChangePercentage = 10.0
                },
                Nexo.Core.Application.Interfaces.Performance.PerformanceTrend.Stable => new Nexo.Core.Application.Interfaces.Monitoring.PerformanceTrend
                {
                    MetricName = "Overall",
                    Direction = TrendDirection.Stable,
                    CurrentValue = 0.5,
                    AverageValue = 0.5,
                    MinValue = 0.4,
                    MaxValue = 0.6,
                    ChangePercentage = 0.0
                },
                Nexo.Core.Application.Interfaces.Performance.PerformanceTrend.Degrading => new Nexo.Core.Application.Interfaces.Monitoring.PerformanceTrend
                {
                    MetricName = "Overall",
                    Direction = TrendDirection.Down,
                    CurrentValue = 0.0,
                    AverageValue = 0.3,
                    MinValue = 0.0,
                    MaxValue = 0.5,
                    ChangePercentage = -15.0
                },
                _ => new Nexo.Core.Application.Interfaces.Monitoring.PerformanceTrend
                {
                    MetricName = "Overall",
                    Direction = TrendDirection.Stable,
                    CurrentValue = 0.5,
                    AverageValue = 0.5,
                    MinValue = 0.4,
                    MaxValue = 0.6,
                    ChangePercentage = 0.0
                }
            };
        }

        private async Task MonitorPerformanceAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var trends = await _performanceOptimizer.GetPerformanceTrendsAsync(
                        TimeSpan.FromMinutes(5), cancellationToken);

                    // Check for performance degradation
                    if (trends.OverallPerformanceTrend == Nexo.Core.Application.Interfaces.Performance.PerformanceTrend.Degrading)
                    {
                        await CreateAlertAsync(new MonitoringAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Category = "Performance",
                            Severity = AlertSeverity.High,
                            Title = "Performance Degradation Detected",
                            Description = "Overall system performance is degrading",
                            Timestamp = DateTimeOffset.UtcNow,
                            Source = "PerformanceMonitor"
                        });
                    }

                    // Check cache hit rate
                    if (trends.CacheHitRateTrend == Nexo.Core.Application.Interfaces.Performance.PerformanceTrend.Degrading)
                    {
                        await CreateAlertAsync(new MonitoringAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Category = "Cache",
                            Severity = AlertSeverity.Medium,
                            Title = "Cache Hit Rate Declining",
                            Description = "Cache hit rate is declining, performance may be affected",
                            Timestamp = DateTimeOffset.UtcNow,
                            Source = "CacheMonitor"
                        });
                    }

                    await Task.Delay(configuration.PerformanceCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring performance");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
        }

        private async Task MonitorSecurityAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);

                    // Check compliance status
                    if (!complianceStatus.IsCompliant)
                    {
                        await CreateAlertAsync(new MonitoringAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Category = "Security",
                            Severity = AlertSeverity.Critical,
                            Title = "Compliance Violation",
                            Description = $"System is not compliant. Score: {complianceStatus.OverallComplianceScore:F1}/100",
                            Timestamp = DateTimeOffset.UtcNow,
                            Source = "SecurityAuditor"
                        });
                    }

                    await Task.Delay(configuration.SecurityCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring security");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
        }

        private async Task MonitorComplianceAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Simulate compliance monitoring
                    var complianceStatus = await _securityAuditor.GetSecurityComplianceStatusAsync(cancellationToken);

                    if (complianceStatus.OverallComplianceScore < 80)
                    {
                        await CreateAlertAsync(new MonitoringAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Category = "Compliance",
                            Severity = AlertSeverity.High,
                            Title = "Low Compliance Score",
                            Description = $"Compliance score is below threshold: {complianceStatus.OverallComplianceScore:F1}/100",
                            Timestamp = DateTimeOffset.UtcNow,
                            Source = "ComplianceMonitor"
                        });
                    }

                    await Task.Delay(configuration.ComplianceCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring compliance");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
        }

        private async Task MonitorSystemHealthAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var systemHealth = await GetSystemHealthAsync(cancellationToken);

                    if (!systemHealth.IsHealthy)
                    {
                        await CreateAlertAsync(new MonitoringAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Category = "System",
                            Severity = AlertSeverity.Critical,
                            Title = "System Health Issue",
                            Description = systemHealth.ErrorMessage ?? "System health check failed",
                            Timestamp = DateTimeOffset.UtcNow,
                            Source = "SystemHealthMonitor"
                        });
                    }

                    await Task.Delay(configuration.SystemHealthCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring system health");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
        }

        private async Task MonitorBusinessMetricsAsync(
            MonitoringConfiguration configuration,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Simulate business metrics monitoring
                    // In a real implementation, this would monitor business-specific metrics
                    
                    await Task.Delay(configuration.BusinessMetricsCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring business metrics");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
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

        private Task CreateAlertAsync(MonitoringAlert alert)
        {
            lock (_lock)
            {
                _activeAlerts[alert.Id] = alert;
            }

            _logger.LogWarning("Alert created: {AlertId} - {Title} ({Severity})", 
                alert.Id, alert.Title, alert.Severity);
            
            return Task.CompletedTask;
        }

        #endregion
    }
}
