using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Enums.FeatureFactory;
using Nexo.Core.Domain.Entities.Monitoring;
using Nexo.Core.Domain.Enums.Monitoring;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Monitoring.Components;

namespace Nexo.Core.Application.Services.Monitoring
{
    /// <summary>
    /// Orchestrator for production monitoring that delegates to specialized monitoring components.
    /// </summary>
    public class ProductionMonitoringService : IProductionMonitoringService
    {
        private readonly ILogger<ProductionMonitoringService> _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly IAlertingService _alertingService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly MonitoringInitializer _monitoringInitializer;
        private readonly MonitoringCollector _monitoringCollector;
        private readonly MonitoringAnalyzer _monitoringAnalyzer;
        private readonly MonitoringReporter _monitoringReporter;

        public ProductionMonitoringService(
            ILogger<ProductionMonitoringService> logger,
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
            
            _monitoringInitializer = new MonitoringInitializer(logger, metricsCollector, alertingService, analyticsService, healthCheckService);
            _monitoringCollector = new MonitoringCollector(logger, metricsCollector);
            _monitoringAnalyzer = new MonitoringAnalyzer(logger, analyticsService);
            _monitoringReporter = new MonitoringReporter(logger, alertingService);
        }

        public async Task<bool> StartMonitoringAsync()
        {
            try
            {
                _logger.LogInformation("Starting production monitoring");
                return await _monitoringCollector.StartCollectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start monitoring");
                return false;
            }
        }

        public async Task<bool> StopMonitoringAsync()
        {
            try
            {
                _logger.LogInformation("Stopping production monitoring");
                return await _monitoringCollector.StopCollectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop monitoring");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetHealthStatusAsync()
        {
            try
            {
                return await _monitoringAnalyzer.GetHealthStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health status");
                return new Dictionary<string, object> { { "error", ex.Message } };
            }
        }

        public async Task<List<string>> GetAlertsAsync()
        {
            try
            {
                return await _monitoringReporter.GetActiveAlertsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alerts");
                return new List<string> { $"Error retrieving alerts: {ex.Message}" };
            }
        }

        public async Task<bool> SendAlertAsync(string message, AlertLevel level)
        {
            try
            {
                return await _monitoringReporter.SendAlertAsync(message, level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert");
                return false;
            }
        }

        /// <summary>
        /// Initializes the production monitoring system
        /// </summary>
        public async Task<MonitoringInitializationResult> InitializeAsync(MonitoringConfiguration config)
        {
            return await _monitoringInitializer.InitializeAsync(config);
        }

        /// <summary>
        /// Collects comprehensive monitoring data
        /// </summary>
        public async Task<MonitoringData> CollectMonitoringDataAsync()
        {
            return await _monitoringCollector.CollectDataAsync();
        }

        /// <summary>
        /// Analyzes monitoring data for insights
        /// </summary>
        public async Task<MonitoringAnalysis> AnalyzeMonitoringDataAsync(MonitoringData data)
        {
            return await _monitoringAnalyzer.AnalyzeDataAsync(data);
        }

        /// <summary>
        /// Generates monitoring reports
        /// </summary>
        public async Task<MonitoringReport> GenerateReportAsync(MonitoringAnalysis analysis)
        {
            return await _monitoringReporter.GenerateReportAsync(analysis);
        }

        /// <summary>
        /// Configures monitoring thresholds and alerts
        /// </summary>
        public async Task<bool> ConfigureMonitoringAsync(MonitoringConfiguration config)
        {
            return await _monitoringInitializer.ConfigureAsync(config);
        }

        /// <summary>
        /// Gets performance metrics for a specific time range
        /// </summary>
        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(DateTime startTime, DateTime endTime)
        {
            return await _monitoringCollector.GetPerformanceMetricsAsync(startTime, endTime);
        }

        /// <summary>
        /// Gets system health status
        /// </summary>
        public async Task<SystemHealthStatus> GetSystemHealthAsync()
        {
            return await _monitoringAnalyzer.GetSystemHealthAsync();
        }

        /// <summary>
        /// Sends custom alert with context
        /// </summary>
        public async Task<bool> SendContextualAlertAsync(string message, AlertLevel level, Dictionary<string, object> context)
        {
            return await _monitoringReporter.SendContextualAlertAsync(message, level, context);
        }
    }
}
