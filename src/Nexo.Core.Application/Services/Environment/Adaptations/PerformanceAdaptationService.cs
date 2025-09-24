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
    public class PerformanceAdaptationService
    {
        private readonly ILogger<PerformanceAdaptationService> _logger;
        private readonly IConfigurationManager _configurationManager;

        public PerformanceAdaptationService(
            ILogger<PerformanceAdaptationService> logger,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        }

        public async Task ApplyPerformanceAdaptationsAsync(EnvironmentProfile environment)
        {
            try
            {
                _logger.LogInformation("Applying performance adaptations for {EnvironmentType}", environment.EnvironmentType);

                var performanceSettings = GetPerformanceSettings(environment);
                
                foreach (var setting in performanceSettings)
                {
                    await _configurationManager.SetConfigurationAsync(setting.Key, setting.Value);
                }

                _logger.LogInformation("Performance adaptations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying performance adaptations");
                throw;
            }
        }

        private Dictionary<string, object> GetPerformanceSettings(EnvironmentProfile environment)
        {
            var settings = new Dictionary<string, object>();

            switch (environment.EnvironmentType)
            {
                case EnvironmentType.Development:
                    settings.Add("ThreadPoolMinThreads", 1);
                    settings.Add("ThreadPoolMaxThreads", 10);
                    settings.Add("GarbageCollectionMode", "Interactive");
                    settings.Add("ConnectionPoolSize", 5);
                    settings.Add("RequestTimeoutSeconds", 30);
                    break;

                case EnvironmentType.Testing:
                    settings.Add("ThreadPoolMinThreads", 2);
                    settings.Add("ThreadPoolMaxThreads", 20);
                    settings.Add("GarbageCollectionMode", "Interactive");
                    settings.Add("ConnectionPoolSize", 10);
                    settings.Add("RequestTimeoutSeconds", 15);
                    break;

                case EnvironmentType.Staging:
                    settings.Add("ThreadPoolMinThreads", 4);
                    settings.Add("ThreadPoolMaxThreads", 50);
                    settings.Add("GarbageCollectionMode", "Server");
                    settings.Add("ConnectionPoolSize", 25);
                    settings.Add("RequestTimeoutSeconds", 60);
                    break;

                case EnvironmentType.Production:
                    settings.Add("ThreadPoolMinThreads", 8);
                    settings.Add("ThreadPoolMaxThreads", 100);
                    settings.Add("GarbageCollectionMode", "Server");
                    settings.Add("ConnectionPoolSize", 50);
                    settings.Add("RequestTimeoutSeconds", 120);
                    break;

                default:
                    _logger.LogWarning("Unknown environment type: {EnvironmentType}", environment.EnvironmentType);
                    break;
            }

            return settings;
        }
    }
}
