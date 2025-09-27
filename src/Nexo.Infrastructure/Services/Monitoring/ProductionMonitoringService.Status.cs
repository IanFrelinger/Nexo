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
    /// Monitoring status and health check functionality
    /// </summary>
    public partial class ProductionMonitoringService : IProductionMonitoringService
    {
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
    }
}
