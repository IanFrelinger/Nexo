using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums.Environment;

namespace Nexo.Core.Application.Services.Environment.Adaptations
{
    public partial class MonitoringAdaptationService
    {
        private readonly ILogger<MonitoringAdaptationService> _logger;
        private readonly IConfigurationManager _configurationManager;

        public MonitoringAdaptationService(
            ILogger<MonitoringAdaptationService> logger,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public async Task ApplyMonitoringAdaptationsAsync(EnvironmentProfile environment)
        {
            try
            {
                _logger.LogInformation("Applying monitoring adaptations for {EnvironmentType}", environment.EnvironmentType);

                var monitoringSettings = GetMonitoringSettings(environment);
                
                foreach (var setting in monitoringSettings)
                {
                    await _configurationManager.SetConfigurationAsync(setting.Key, setting.Value);
                }

                _logger.LogInformation("Monitoring adaptations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying monitoring adaptations");
                throw;
            }
        }

        private Dictionary<string, object> GetMonitoringSettings(EnvironmentProfile environment)
        {
            var settings = new Dictionary<string, object>();

            switch (environment.EnvironmentType)
            {
                case EnvironmentType.Development:
                    settings.Add("EnableMetrics", true);
                    settings.Add("EnableHealthChecks", true);
                    settings.Add("EnableTracing", true);
                    settings.Add("MetricsCollectionIntervalSeconds", 30);
                    settings.Add("EnableDetailedMetrics", true);
                    break;

                case EnvironmentType.Testing:
                    settings.Add("EnableMetrics", true);
                    settings.Add("EnableHealthChecks", true);
                    settings.Add("EnableTracing", true);
                    settings.Add("MetricsCollectionIntervalSeconds", 15);
                    settings.Add("EnableDetailedMetrics", true);
                    break;

                case EnvironmentType.Staging:
                    settings.Add("EnableMetrics", true);
                    settings.Add("EnableHealthChecks", true);
                    settings.Add("EnableTracing", true);
                    settings.Add("MetricsCollectionIntervalSeconds", 60);
                    settings.Add("EnableDetailedMetrics", false);
                    break;

                case EnvironmentType.Production:
                    settings.Add("EnableMetrics", true);
                    settings.Add("EnableHealthChecks", true);
                    settings.Add("EnableTracing", true);
                    settings.Add("MetricsCollectionIntervalSeconds", 300);
                    settings.Add("EnableDetailedMetrics", false);
                    settings.Add("EnableAlerting", true);
                    settings.Add("EnableDashboard", true);
                    break;

                default:
                    _logger.LogWarning("Unknown environment type: {EnvironmentType}", environment.EnvironmentType);
                    break;
            }

            return settings;
        }
    }
}
