using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.Monitoring;
using Nexo.Core.Domain.Enums.Monitoring;
using System;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring.Components
{
    public class MonitoringInitializer
    {
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly IAlertingService _alertingService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IHealthCheckService _healthCheckService;

        public MonitoringInitializer(
            ILogger logger,
            IMetricsCollector metricsCollector,
            IAlertingService alertingService,
            IAnalyticsService analyticsService,
            IHealthCheckService healthCheckService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _alertingService = alertingService ?? throw new ArgumentNullException(nameof(alertingService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        }

        public async Task<MonitoringInitializationResult> InitializeAsync(MonitoringConfiguration config)
        {
            _logger.LogInformation("Initializing production monitoring system");

            try
            {
                // Initialize metrics collection
                await _metricsCollector.InitializeAsync();

                // Initialize alerting system
                await _alertingService.InitializeAsync();

                // Initialize analytics
                await _analyticsService.InitializeAsync();

                // Initialize health checks
                await _healthCheckService.InitializeAsync();

                // Configure monitoring based on config
                await ConfigureMonitoringSettingsAsync(config);

                _logger.LogInformation("Production monitoring system initialized successfully");

                return new MonitoringInitializationResult
                {
                    Success = true,
                    Message = "Monitoring system initialized successfully",
                    InitializedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize monitoring system");
                return new MonitoringInitializationResult
                {
                    Success = false,
                    Message = $"Initialization failed: {ex.Message}",
                    InitializedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> ConfigureAsync(MonitoringConfiguration config)
        {
            try
            {
                _logger.LogInformation("Configuring monitoring system");

                // Configure metrics collection
                await _metricsCollector.ConfigureAsync(config.MetricsConfiguration);

                // Configure alerting
                await _alertingService.ConfigureAsync(config.AlertingConfiguration);

                // Configure analytics
                await _analyticsService.ConfigureAsync(config.AnalyticsConfiguration);

                // Configure health checks
                await _healthCheckService.ConfigureAsync(config.HealthCheckConfiguration);

                _logger.LogInformation("Monitoring system configured successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure monitoring system");
                return false;
            }
        }

        private async Task ConfigureMonitoringSettingsAsync(MonitoringConfiguration config)
        {
            // Set up monitoring intervals
            if (config.MonitoringInterval > 0)
            {
                _logger.LogInformation("Setting monitoring interval to {Interval} seconds", config.MonitoringInterval);
            }

            // Configure alert thresholds
            if (config.AlertThresholds != null)
            {
                _logger.LogInformation("Configuring alert thresholds");
                foreach (var threshold in config.AlertThresholds)
                {
                    _logger.LogDebug("Setting threshold for {Metric}: {Value}", threshold.MetricName, threshold.ThresholdValue);
                }
            }

            // Configure data retention
            if (config.DataRetentionDays > 0)
            {
                _logger.LogInformation("Setting data retention to {Days} days", config.DataRetentionDays);
            }

            await Task.CompletedTask;
        }
    }
}
