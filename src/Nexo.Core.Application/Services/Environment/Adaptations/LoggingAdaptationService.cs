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
    public class LoggingAdaptationService
    {
        private readonly ILogger<LoggingAdaptationService> _logger;
        private readonly IConfigurationManager _configurationManager;

        public LoggingAdaptationService(
            ILogger<LoggingAdaptationService> logger,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public async Task ApplyLoggingAdaptationsAsync(EnvironmentProfile environment)
        {
            try
            {
                _logger.LogInformation("Applying logging adaptations for {EnvironmentType}", environment.EnvironmentType);

                var loggingSettings = GetLoggingSettings(environment);
                
                foreach (var setting in loggingSettings)
                {
                    await _configurationManager.SetConfigurationAsync(setting.Key, setting.Value);
                }

                _logger.LogInformation("Logging adaptations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying logging adaptations");
                throw;
            }
        }

        private Dictionary<string, object> GetLoggingSettings(EnvironmentProfile environment)
        {
            var settings = new Dictionary<string, object>();

            switch (environment.EnvironmentType)
            {
                case EnvironmentType.Development:
                    settings.Add("LogLevel", "Debug");
                    settings.Add("EnableConsoleLogging", true);
                    settings.Add("EnableFileLogging", true);
                    settings.Add("EnableStructuredLogging", true);
                    settings.Add("LogRetentionDays", 7);
                    settings.Add("EnablePerformanceLogging", true);
                    break;

                case EnvironmentType.Testing:
                    settings.Add("LogLevel", "Information");
                    settings.Add("EnableConsoleLogging", true);
                    settings.Add("EnableFileLogging", true);
                    settings.Add("EnableStructuredLogging", true);
                    settings.Add("LogRetentionDays", 3);
                    settings.Add("EnablePerformanceLogging", true);
                    break;

                case EnvironmentType.Staging:
                    settings.Add("LogLevel", "Warning");
                    settings.Add("EnableConsoleLogging", false);
                    settings.Add("EnableFileLogging", true);
                    settings.Add("EnableStructuredLogging", true);
                    settings.Add("LogRetentionDays", 14);
                    settings.Add("EnablePerformanceLogging", true);
                    break;

                case EnvironmentType.Production:
                    settings.Add("LogLevel", "Error");
                    settings.Add("EnableConsoleLogging", false);
                    settings.Add("EnableFileLogging", true);
                    settings.Add("EnableStructuredLogging", true);
                    settings.Add("LogRetentionDays", 30);
                    settings.Add("EnablePerformanceLogging", false);
                    settings.Add("EnableAuditLogging", true);
                    break;

                default:
                    _logger.LogWarning("Unknown environment type: {EnvironmentType}", environment.EnvironmentType);
                    break;
            }

            return settings;
        }
    }
}
