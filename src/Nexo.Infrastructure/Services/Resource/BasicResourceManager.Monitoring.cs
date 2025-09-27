using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Infrastructure.Services.Resource
{
    /// <summary>
    /// Monitoring and health assessment functionality
    /// </summary>
    public partial class BasicResourceManager
    {
        private async Task<ResourceHealthStatus> AssessHealthAsync(CancellationToken cancellationToken)
        {
            var healthStatus = new ResourceHealthStatus
            {
                LastCheckTime = DateTime.UtcNow
            };

            var usage = await GetUsageAsync(cancellationToken);
            var overallHealth = ResourceHealth.Healthy;

            foreach (var kvp in usage.UtilizationByType)
            {
                var resourceType = kvp.Key;
                var utilization = kvp.Value;

                switch (utilization)
                {
                    case > 95:
                        healthStatus.StatusByType[resourceType] = ResourceHealth.Unhealthy;
                        overallHealth = ResourceHealth.Unhealthy;
                        break;
                    case > 80:
                    {
                        healthStatus.StatusByType[resourceType] = ResourceHealth.Degraded;
                        if (overallHealth == ResourceHealth.Healthy)
                        {
                            overallHealth = ResourceHealth.Degraded;
                        }

                        break;
                    }
                    default:
                        healthStatus.StatusByType[resourceType] = ResourceHealth.Healthy;
                        break;
                }
            }

            healthStatus.OverallStatus = overallHealth;
            return healthStatus;
        }

        private void MonitorResources(object? state)
        {
            try
            {
                // Update CPU and memory metrics
                if (_cpuCounter != null && OperatingSystem.IsWindows())
                {
                    var cpuUsage = _cpuCounter.NextValue();
                    if (!_metrics.ContainsKey(ResourceType.CPU))
                        _metrics[ResourceType.CPU] = new ResourceMetrics();

                    _metrics[ResourceType.CPU].AverageUtilization = cpuUsage;
                    _metrics[ResourceType.CPU].PeakUtilization = Math.Max(_metrics[ResourceType.CPU].PeakUtilization, cpuUsage);
                }

                if (_memoryCounter is not null && OperatingSystem.IsWindows())
                {
                    var availableMemory = _memoryCounter.NextValue();
                    // Convert to utilization percentage (assuming total memory from limits)
                    if (_limits.MaximumByType.TryGetValue(ResourceType.Memory, out var totalMemory))
                    {
                        var totalMemoryMb = totalMemory / (1024 * 1024);
                        var memoryUtilization = ((totalMemoryMb - availableMemory) / totalMemoryMb) * 100;

                        if (!_metrics.ContainsKey(ResourceType.Memory))
                            _metrics[ResourceType.Memory] = new ResourceMetrics();

                        _metrics[ResourceType.Memory].AverageUtilization = memoryUtilization;
                        _metrics[ResourceType.Memory].PeakUtilization = Math.Max(_metrics[ResourceType.Memory].PeakUtilization, memoryUtilization);
                    }
                }

                // Check for alerts
                CheckForAlerts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource monitoring");
            }
        }

        private void CheckForAlerts()
        {
            // Clear old alerts
            _alerts.RemoveAll(a => a.Timestamp < DateTime.UtcNow.AddMinutes(-5));

            // Check for high utilization
            foreach (var alert in from kvp in _metrics let resourceType = kvp.Key let metrics = kvp.Value where metrics.AverageUtilization > 90 select new ResourceAlert
                     {
                         AlertId = Guid.NewGuid().ToString(),
                         Type = ResourceAlertType.HighUtilization,
                         Severity = ResourceAlertSeverity.Warning,
                         Message = $"High {resourceType} utilization: {metrics.AverageUtilization:F1}%",
                         ResourceType = resourceType,
                         Timestamp = DateTime.UtcNow
                     } into alert where !_alerts.Any(a => a.Type == alert.Type && a.ResourceType == alert.ResourceType) select alert)
            {
                _alerts.Add(alert);
                _logger.LogWarning("Resource alert: {Message}", alert.Message);
            }
        }
    }
}
