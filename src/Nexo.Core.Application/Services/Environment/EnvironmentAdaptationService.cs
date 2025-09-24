using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums.Environment;
using Nexo.Core.Application.Services.Environment.Adaptations;

namespace Nexo.Core.Application.Services.Environment
{
    /// <summary>
    /// Orchestrator for environment adaptation that delegates to specialized adaptation services.
    /// </summary>
    public class EnvironmentAdaptationService : IEnvironmentAdaptationService
    {
        private readonly IEnvironmentDetector _environmentDetector;
        private readonly IConfigurationManager _configurationManager;
        private readonly IAdaptationEngine _adaptationEngine;
        private readonly ILogger<EnvironmentAdaptationService> _logger;
        private readonly IEnvironmentDataStore _environmentStore;
        private readonly ConfigurationAdaptationService _configurationAdaptationService;
        private readonly PerformanceAdaptationService _performanceAdaptationService;
        private readonly SecurityAdaptationService _securityAdaptationService;
        private readonly LoggingAdaptationService _loggingAdaptationService;
        private readonly MonitoringAdaptationService _monitoringAdaptationService;

        public EnvironmentAdaptationService(
            IEnvironmentDetector environmentDetector,
            IConfigurationManager configurationManager,
            IAdaptationEngine adaptationEngine,
            ILogger<EnvironmentAdaptationService> logger,
            IEnvironmentDataStore environmentStore,
            ConfigurationAdaptationService configurationAdaptationService,
            PerformanceAdaptationService performanceAdaptationService,
            SecurityAdaptationService securityAdaptationService,
            LoggingAdaptationService loggingAdaptationService,
            MonitoringAdaptationService monitoringAdaptationService)
        {
            _environmentDetector = environmentDetector ?? throw new ArgumentNullException(nameof(environmentDetector));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            _adaptationEngine = adaptationEngine ?? throw new ArgumentNullException(nameof(adaptationEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environmentStore = environmentStore ?? throw new ArgumentNullException(nameof(environmentStore));
            _configurationAdaptationService = configurationAdaptationService ?? throw new ArgumentNullException(nameof(configurationAdaptationService));
            _performanceAdaptationService = performanceAdaptationService ?? throw new ArgumentNullException(nameof(performanceAdaptationService));
            _securityAdaptationService = securityAdaptationService ?? throw new ArgumentNullException(nameof(securityAdaptationService));
            _loggingAdaptationService = loggingAdaptationService ?? throw new ArgumentNullException(nameof(loggingAdaptationService));
            _monitoringAdaptationService = monitoringAdaptationService ?? throw new ArgumentNullException(nameof(monitoringAdaptationService));
        }

        public async Task AdaptToEnvironmentAsync()
        {
            _logger.LogInformation("Starting environment adaptation");

            var currentEnvironment = await _environmentDetector.DetectCurrentEnvironmentAsync();
            var previousEnvironment = await GetPreviousEnvironmentAsync();

            if (HasEnvironmentChanged(currentEnvironment, previousEnvironment))
            {
                _logger.LogInformation("Environment change detected, applying adaptations");
                await ApplyEnvironmentSpecificAdaptations(currentEnvironment);
                await StorePreviousEnvironment(currentEnvironment);
                await RecordEnvironmentChange(previousEnvironment, currentEnvironment);
            }
            else
            {
                _logger.LogInformation("No environment change detected");
            }
        }

        public async Task ApplyEnvironmentAdaptationsAsync(EnvironmentProfile environment)
        {
            _logger.LogInformation("Applying environment adaptations for {EnvironmentId}", environment.EnvironmentId);

            // Apply configuration adaptations
            await _configurationAdaptationService.ApplyConfigurationAdaptationsAsync(environment);

            // Apply performance adaptations
            await _performanceAdaptationService.ApplyPerformanceAdaptationsAsync(environment);

            // Apply security adaptations
            await _securityAdaptationService.ApplySecurityAdaptationsAsync(environment);

            // Apply logging adaptations
            await _loggingAdaptationService.ApplyLoggingAdaptationsAsync(environment);

            // Apply monitoring adaptations
            await _monitoringAdaptationService.ApplyMonitoringAdaptationsAsync(environment);
        }

        private async Task<EnvironmentProfile?> GetPreviousEnvironmentAsync()
        {
            try
            {
                return await _environmentStore.GetPreviousEnvironmentAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve previous environment");
                return null;
            }
        }

        private bool HasEnvironmentChanged(EnvironmentProfile current, EnvironmentProfile? previous)
        {
            if (previous == null) return true;
            return current.EnvironmentId != previous.EnvironmentId || 
                   current.EnvironmentType != previous.EnvironmentType;
        }

        private async Task ApplyEnvironmentSpecificAdaptations(EnvironmentProfile environment)
        {
            await ApplyEnvironmentAdaptationsAsync(environment);
        }

        private async Task StorePreviousEnvironment(EnvironmentProfile environment)
        {
            try
            {
                await _environmentStore.StoreEnvironmentAsync(environment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store environment profile");
            }
        }

        private async Task RecordEnvironmentChange(EnvironmentProfile? previous, EnvironmentProfile current)
        {
            try
            {
                var change = new EnvironmentChange
                {
                    PreviousEnvironment = previous?.EnvironmentId,
                    CurrentEnvironment = current.EnvironmentId,
                    ChangeTime = DateTime.UtcNow,
                    ChangeType = previous == null ? EnvironmentChangeType.Initial : EnvironmentChangeType.Transition
                };

                await _environmentStore.RecordEnvironmentChangeAsync(change);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record environment change");
            }
        }
    }
}
