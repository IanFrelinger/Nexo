using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Monitoring;

namespace Nexo.Infrastructure.Services.Monitoring
{
    /// <summary>
    /// Alert management functionality
    /// </summary>
    public partial class ProductionMonitoringService : IProductionMonitoringService
    {
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
    }
}
