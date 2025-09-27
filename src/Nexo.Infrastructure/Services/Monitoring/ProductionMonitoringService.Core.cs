using System;
using System.Collections.Generic;
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
    /// Core production monitoring functionality
    /// </summary>
    public partial class ProductionMonitoringService : IProductionMonitoringService
    {
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
    }
}
