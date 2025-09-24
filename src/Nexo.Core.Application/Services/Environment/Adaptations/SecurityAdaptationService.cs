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
    public class SecurityAdaptationService
    {
        private readonly ILogger<SecurityAdaptationService> _logger;
        private readonly IConfigurationManager _configurationManager;

        public SecurityAdaptationService(
            ILogger<SecurityAdaptationService> logger,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public async Task ApplySecurityAdaptationsAsync(EnvironmentProfile environment)
        {
            try
            {
                _logger.LogInformation("Applying security adaptations for {EnvironmentType}", environment.EnvironmentType);

                var securitySettings = GetSecuritySettings(environment);
                
                foreach (var setting in securitySettings)
                {
                    await _configurationManager.SetConfigurationAsync(setting.Key, setting.Value);
                }

                _logger.LogInformation("Security adaptations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying security adaptations");
                throw;
            }
        }

        private Dictionary<string, object> GetSecuritySettings(EnvironmentProfile environment)
        {
            var settings = new Dictionary<string, object>();

            switch (environment.EnvironmentType)
            {
                case EnvironmentType.Development:
                    settings.Add("RequireHttps", false);
                    settings.Add("EnableCors", true);
                    settings.Add("SessionTimeoutMinutes", 480); // 8 hours
                    settings.Add("PasswordPolicy", "Relaxed");
                    settings.Add("EnableSecurityHeaders", false);
                    break;

                case EnvironmentType.Testing:
                    settings.Add("RequireHttps", true);
                    settings.Add("EnableCors", true);
                    settings.Add("SessionTimeoutMinutes", 60);
                    settings.Add("PasswordPolicy", "Standard");
                    settings.Add("EnableSecurityHeaders", true);
                    break;

                case EnvironmentType.Staging:
                    settings.Add("RequireHttps", true);
                    settings.Add("EnableCors", false);
                    settings.Add("SessionTimeoutMinutes", 30);
                    settings.Add("PasswordPolicy", "Strict");
                    settings.Add("EnableSecurityHeaders", true);
                    break;

                case EnvironmentType.Production:
                    settings.Add("RequireHttps", true);
                    settings.Add("EnableCors", false);
                    settings.Add("SessionTimeoutMinutes", 15);
                    settings.Add("PasswordPolicy", "Strict");
                    settings.Add("EnableSecurityHeaders", true);
                    settings.Add("EnableRateLimiting", true);
                    settings.Add("MaxRequestSizeMB", 10);
                    break;

                default:
                    _logger.LogWarning("Unknown environment type: {EnvironmentType}", environment.EnvironmentType);
                    break;
            }

            return settings;
        }
    }
}
