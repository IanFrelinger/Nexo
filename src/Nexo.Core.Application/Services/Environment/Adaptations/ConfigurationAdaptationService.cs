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
    public class ConfigurationAdaptationService
    {
        private readonly ILogger<ConfigurationAdaptationService> _logger;
        private readonly IConfigurationManager _configurationManager;

        public ConfigurationAdaptationService(
            ILogger<ConfigurationAdaptationService> logger,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public async Task ApplyConfigurationAdaptationsAsync(EnvironmentProfile environment)
        {
            try
            {
                _logger.LogInformation("Applying configuration adaptations for {EnvironmentType}", environment.EnvironmentType);

                var configurations = GetEnvironmentSpecificConfigurations(environment);
                
                foreach (var config in configurations)
                {
                    await _configurationManager.SetConfigurationAsync(config.Key, config.Value);
                }

                _logger.LogInformation("Configuration adaptations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying configuration adaptations");
                throw;
            }
        }

        private Dictionary<string, object> GetEnvironmentSpecificConfigurations(EnvironmentProfile environment)
        {
            var configurations = new Dictionary<string, object>();

            switch (environment.EnvironmentType)
            {
                case EnvironmentType.Development:
                    configurations.Add("LogLevel", "Debug");
                    configurations.Add("EnableDetailedLogging", true);
                    configurations.Add("EnablePerformanceCounters", true);
                    configurations.Add("DatabaseConnectionTimeout", 30);
                    configurations.Add("CacheExpirationMinutes", 5);
                    break;

                case EnvironmentType.Testing:
                    configurations.Add("LogLevel", "Information");
                    configurations.Add("EnableDetailedLogging", false);
                    configurations.Add("EnablePerformanceCounters", true);
                    configurations.Add("DatabaseConnectionTimeout", 15);
                    configurations.Add("CacheExpirationMinutes", 1);
                    break;

                case EnvironmentType.Staging:
                    configurations.Add("LogLevel", "Warning");
                    configurations.Add("EnableDetailedLogging", false);
                    configurations.Add("EnablePerformanceCounters", true);
                    configurations.Add("DatabaseConnectionTimeout", 60);
                    configurations.Add("CacheExpirationMinutes", 30);
                    break;

                case EnvironmentType.Production:
                    configurations.Add("LogLevel", "Error");
                    configurations.Add("EnableDetailedLogging", false);
                    configurations.Add("EnablePerformanceCounters", false);
                    configurations.Add("DatabaseConnectionTimeout", 120);
                    configurations.Add("CacheExpirationMinutes", 60);
                    break;

                default:
                    _logger.LogWarning("Unknown environment type: {EnvironmentType}", environment.EnvironmentType);
                    break;
            }

            return configurations;
        }
    }
}
