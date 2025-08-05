using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Provides functionality for managing, validating, saving, and loading AI configuration settings in support of various AI modes.
    /// </summary>
    public class AiConfigurationService : IAiConfigurationService
    {
        /// <summary>
        /// Logger instance used to log informational, warning, error, or debug messages
        /// within the <see cref="AiConfigurationService"/> class. Provides a mechanism for
        /// structured and consistent logging of events during the service's operation.
        /// </summary>
        private readonly ILogger<AiConfigurationService> _logger;

        /// <summary>
        /// Represents the current AI configuration used within the service.
        /// </summary>
        /// <remarks>
        /// This variable holds an instance of <see cref="AiConfiguration"/>,
        /// which defines various settings for AI operations, such as mode, model configuration,
        /// providers, resource allocation, caching, fallback mechanisms, and monitoring parameters.
        /// It is initialized with default configuration values and can be updated or retrieved
        /// through corresponding service methods.
        /// </remarks>
        private AiConfiguration _configuration;

        /// Provides functionality for managing AI configuration settings.
        /// This service initializes and retrieves AI configurations based on
        /// specific modes, such as Development or Production.
        public AiConfigurationService(ILogger<AiConfigurationService> logger)
        {
            _logger = logger;
            _configuration = GetDefaultConfiguration(AiMode.Development);
        }

        /// <summary>
        /// Asynchronously retrieves the current AI configuration.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains
        /// an instance of <see cref="AiConfiguration"/> representing the AI configuration.
        /// </returns>
        public async Task<AiConfiguration> GetConfigurationAsync()
        {
            _logger.LogInformation("Getting AI configuration");
            await Task.CompletedTask;
            return _configuration;
        }

        /// <summary>
        /// Saves the specified AI configuration asynchronously.
        /// </summary>
        /// <param name="configuration">The AI configuration to save.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        public async Task SaveConfigurationAsync(AiConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            _logger.LogInformation("Saving AI configuration");
            await Task.CompletedTask;
            
            _configuration = configuration;
        }

        /// <summary>
        /// Loads the AI configuration for the specified mode asynchronously.
        /// </summary>
        /// <param name="mode">The mode for which the AI configuration is being loaded. Possible values are defined in <see cref="AiMode"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="AiConfiguration"/> for the specified mode.</returns>
        public async Task<AiConfiguration> LoadForModeAsync(AiMode mode)
        {
            _logger.LogInformation("Loading AI configuration for mode: {Mode}", mode);
            await Task.CompletedTask;
            
            var config = GetDefaultConfiguration(mode);
            await SaveConfigurationAsync(config);
            return config;
        }

        /// <summary>
        /// Retrieves the default configuration for the specified AI mode.
        /// </summary>
        /// <param name="mode">The AI mode for which the default configuration is requested.</param>
        /// <returns>The default AI configuration for the specified mode.</returns>
        public AiConfiguration GetDefaultConfiguration(AiMode mode)
        {
            _logger.LogInformation("Getting default AI configuration for mode: {Mode}", mode);

            string modelName;
            int maxInputTokens;
            int maxOutputTokens;
            double temperature;
            int requestTimeoutSeconds;
            int maxConcurrentRequests;
            long maxMemoryUsageBytes;
            double maxCpuUsagePercentage;
            long maxGpuMemoryUsageBytes;
            double maxGpuUsagePercentage;
            PerformanceMode perfMode;
            int targetResponseTimeMs;
            int maxResponseTimeMs;
            int maxBatchSize;
            long maxCacheSizeBytes;
            int defaultExpirationSeconds;
            int maxFallbackAttempts;
            int fallbackDelaySeconds;
            int collectionIntervalSeconds;
            int maxConnectionPoolSize;
            int cacheExpirationSeconds;
            int maxResponseTimeAlert;
            double maxErrorRateAlert;

            switch (mode)
            {
                case AiMode.Development:
                    modelName = "gpt-3.5-turbo";
                    maxInputTokens = 4096;
                    maxOutputTokens = 2048;
                    temperature = 0.3;
                    requestTimeoutSeconds = 30;
                    maxConcurrentRequests = 5;
                    maxMemoryUsageBytes = 1L * 1024L * 1024L * 1024L;
                    maxCpuUsagePercentage = 50.0;
                    maxGpuMemoryUsageBytes = 0L;
                    maxGpuUsagePercentage = 0.0;
                    perfMode = PerformanceMode.Balanced;
                    targetResponseTimeMs = 2000;
                    maxResponseTimeMs = 10000;
                    maxBatchSize = 1;
                    maxCacheSizeBytes = 100L * 1024L * 1024L;
                    defaultExpirationSeconds = 300;
                    maxFallbackAttempts = 1;
                    fallbackDelaySeconds = 1;
                    collectionIntervalSeconds = 60;
                    maxConnectionPoolSize = 5;
                    cacheExpirationSeconds = 300;
                    maxResponseTimeAlert = 5000;
                    maxErrorRateAlert = 0.05;
                    break;
                case AiMode.Production:
                    modelName = "gpt-4";
                    maxInputTokens = 8192;
                    maxOutputTokens = 4096;
                    temperature = 0.5;
                    requestTimeoutSeconds = 60;
                    maxConcurrentRequests = 20;
                    maxMemoryUsageBytes = 2L * 1024L * 1024L * 1024L;
                    maxCpuUsagePercentage = 80.0;
                    maxGpuMemoryUsageBytes = 2L * 1024L * 1024L * 1024L;
                    maxGpuUsagePercentage = 80.0;
                    perfMode = PerformanceMode.Balanced;
                    targetResponseTimeMs = 5000;
                    maxResponseTimeMs = 30000;
                    maxBatchSize = 10;
                    maxCacheSizeBytes = 1L * 1024L * 1024L * 1024L;
                    defaultExpirationSeconds = 3600;
                    maxFallbackAttempts = 3;
                    fallbackDelaySeconds = 5;
                    collectionIntervalSeconds = 30;
                    maxConnectionPoolSize = 20;
                    cacheExpirationSeconds = 3600;
                    maxResponseTimeAlert = 10000;
                    maxErrorRateAlert = 0.02;
                    break;
                case AiMode.AiHeavy:
                    modelName = "gpt-4-turbo";
                    maxInputTokens = 16384;
                    maxOutputTokens = 8192;
                    temperature = 0.7;
                    requestTimeoutSeconds = 120;
                    maxConcurrentRequests = 50;
                    maxMemoryUsageBytes = 8L * 1024L * 1024L * 1024L;
                    maxCpuUsagePercentage = 95.0;
                    maxGpuMemoryUsageBytes = 8L * 1024L * 1024L * 1024L;
                    maxGpuUsagePercentage = 95.0;
                    perfMode = PerformanceMode.Balanced;
                    targetResponseTimeMs = 10000;
                    maxResponseTimeMs = 60000;
                    maxBatchSize = 20;
                    maxCacheSizeBytes = 5L * 1024L * 1024L * 1024L;
                    defaultExpirationSeconds = 7200;
                    maxFallbackAttempts = 5;
                    fallbackDelaySeconds = 10;
                    collectionIntervalSeconds = 15;
                    maxConnectionPoolSize = 50;
                    cacheExpirationSeconds = 7200;
                    maxResponseTimeAlert = 30000;
                    maxErrorRateAlert = 0.01;
                    break;
                default:
                    modelName = "gpt-3.5-turbo";
                    maxInputTokens = 4096;
                    maxOutputTokens = 2048;
                    temperature = 0.3;
                    requestTimeoutSeconds = 30;
                    maxConcurrentRequests = 5;
                    maxMemoryUsageBytes = 1L * 1024L * 1024L * 1024L;
                    maxCpuUsagePercentage = 50.0;
                    maxGpuMemoryUsageBytes = 0L;
                    maxGpuUsagePercentage = 0.0;
                    perfMode = PerformanceMode.Balanced;
                    targetResponseTimeMs = 5000;
                    maxResponseTimeMs = 30000;
                    maxBatchSize = 1;
                    maxCacheSizeBytes = 1L * 1024L * 1024L * 1024L;
                    defaultExpirationSeconds = 300;
                    maxFallbackAttempts = 1;
                    fallbackDelaySeconds = 1;
                    collectionIntervalSeconds = 30;
                    maxConnectionPoolSize = 5;
                    cacheExpirationSeconds = 300;
                    maxResponseTimeAlert = 10000;
                    maxErrorRateAlert = 0.05;
                    break;
            }

            return new AiConfiguration
            {
                Mode = mode,
                Model = new AiModelConfiguration
                {
                    Name = modelName,
                    MaxInputTokens = maxInputTokens,
                    MaxOutputTokens = maxOutputTokens,
                    Temperature = temperature,
                    EnableStreaming = true,
                    RequestTimeoutSeconds = requestTimeoutSeconds
                },
                Resources = new AiResourceConfiguration
                {
                    MaxConcurrentRequests = maxConcurrentRequests,
                    MaxMemoryUsageBytes = maxMemoryUsageBytes,
                    MaxCpuUsagePercentage = maxCpuUsagePercentage,
                    MaxGpuMemoryUsageBytes = maxGpuMemoryUsageBytes,
                    MaxGpuUsagePercentage = maxGpuUsagePercentage,
                    EnableResourceMonitoring = true,
                    ResourceMonitoringIntervalSeconds = 30
                },
                Performance = new AiPerformanceConfiguration
                {
                    Mode = perfMode,
                    TargetResponseTimeMs = targetResponseTimeMs,
                    MaxResponseTimeMs = maxResponseTimeMs
                },
                Caching = new AiCachingConfiguration
                {
                    Enabled = mode != AiMode.Development,
                    MaxCacheSizeBytes = maxCacheSizeBytes,
                    DefaultExpirationSeconds = defaultExpirationSeconds,
                    EvictionPolicy = CacheEvictionPolicy.LeastRecentlyUsed,
                    EnableCompression = mode != AiMode.Development,
                    CompressionLevel = 6
                },
                Fallback = new AiFallbackConfiguration
                {
                    Enabled = mode != AiMode.Development,
                    MaxFallbackAttempts = maxFallbackAttempts,
                    FallbackDelaySeconds = fallbackDelaySeconds,
                    EnableExponentialBackoff = mode != AiMode.Development,
                    EnableOfflineMode = mode != AiMode.Development,
                    EnableCachedResponseFallback = mode != AiMode.Development
                },
                Monitoring = new AiMonitoringConfiguration
                {
                    Enabled = mode != AiMode.Development,
                    CollectionIntervalSeconds = collectionIntervalSeconds,
                    EnableRequestResponseLogging = true,
                    EnablePerformanceMetrics = mode != AiMode.Development,
                    EnableErrorTracking = mode != AiMode.Development,
                    EnableUsageAnalytics = mode != AiMode.Development,
                    EnableHealthChecks = mode != AiMode.Development,
                    AlertThresholds = new AiAlertThresholds
                    {
                        MaxResponseTimeMs = maxResponseTimeAlert,
                        MaxErrorRate = maxErrorRateAlert
                    }
                }
            };
        }

        /// <summary>
        /// Validates the provided AI configuration asynchronously and returns the validation result,
        /// indicating whether the configuration adheres to the expected rules and requirements.
        /// </summary>
        /// <param name="configuration">The AI configuration to be validated. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains
        /// an <see cref="AiConfigurationValidationResult"/> with validation status and a collection of errors if any.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="configuration"/> is null.</exception>
        public async Task<AiConfigurationValidationResult> ValidateAsync(AiConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            _logger.LogInformation("Validating AI configuration");
            await Task.CompletedTask;
            
            var result = new AiConfigurationValidationResult { IsValid = true };
            
            // Validate model configuration
            if (configuration.Model != null)
            {
                if (string.IsNullOrEmpty(configuration.Model.Name))
                {
                    result.Errors.Add(new AiConfigurationValidationError
                    {
                        Code = "MODEL_NAME_EMPTY",
                        Message = "Model name is required",
                        FieldPath = "model.name",
                        Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
                
                if (configuration.Model.MaxInputTokens <= 0)
                {
                    result.Errors.Add(new AiConfigurationValidationError
                    {
                        Code = "MAX_INPUT_TOKENS_INVALID",
                        Message = "Max input tokens must be greater than 0",
                        FieldPath = "model.maxInputTokens",
                        Severity = Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
                
                if (configuration.Model.MaxOutputTokens <= 0)
                {
                    result.Errors.Add(new AiConfigurationValidationError
                    {
                        Code = "MAX_OUTPUT_TOKENS_INVALID",
                        Message = "Max output tokens must be greater than 0",
                        FieldPath = "model.maxOutputTokens",
                        Severity = Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
            }
            else
            {
                result.Errors.Add(new AiConfigurationValidationError
                {
                    Code = "MODEL_NULL",
                    Message = "Model configuration is required",
                    FieldPath = "model",
                    Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
                });
                result.IsValid = false;
            }
            
            // Validate resource configuration
            if (configuration.Resources != null)
            {
                if (configuration.Resources.MaxConcurrentRequests <= 0)
                {
                    result.Errors.Add(new AiConfigurationValidationError
                    {
                        Code = "MAX_CONCURRENT_REQUESTS_INVALID",
                        Message = "Max concurrent requests must be greater than 0",
                        FieldPath = "resources.maxConcurrentRequests",
                        Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
                
                if (configuration.Resources.MaxMemoryUsageBytes <= 0)
                {
                    result.Errors.Add(new AiConfigurationValidationError
                    {
                        Code = "MAX_MEMORY_USAGE_INVALID",
                        Message = "Max memory usage must be greater than 0",
                        FieldPath = "resources.maxMemoryUsageBytes",
                        Severity = Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
            }
            
            if (!result.IsValid)
            {
                _logger.LogWarning("Configuration validation failed: {ErrorCount} errors", result.Errors.Count);
            }
            
            return result;
        }

        /// <summary>
        /// Merges multiple AIConfiguration objects into a single configuration.
        /// If the configurations list is empty, returns a default configuration.
        /// If the configurations list is not empty, returns the last configuration in the list.
        /// </summary>
        /// <param name="configurations">A collection of AIConfiguration objects to merge.</param>
        /// <returns>An AIConfiguration object representing the merged result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the configurations parameter is null.</exception>
        public async Task<AiConfiguration> MergeAsync(IEnumerable<AiConfiguration> configurations)
        {
            if (configurations != null)
            {
                _logger.LogInformation("Merging AI configurations");
                await Task.CompletedTask;

                var configList = configurations.ToList();
                return !configList.Any() ? GetDefaultConfiguration(AiMode.Development) :
                    // For now, return the last configuration
                    // In a real implementation, this would merge all configurations
                    configList.Last();
            }

            throw new ArgumentNullException(nameof(configurations));
        }

        /// <summary>
        /// Retrieves the path of the AI configuration file.
        /// </summary>
        /// <returns>The path to the AI configuration file as a string.</returns>
        public string GetConfigurationPath()
        {
            return "ai-config.json";
        }

        /// <summary>
        /// Checks whether the AI configuration exists.
        /// </summary>
        /// <returns>A Task that represents the asynchronous operation, containing a boolean indicating whether the configuration exists.</returns>
        public async Task<bool> ExistsAsync()
        {
            _logger.LogInformation("Checking if AI configuration exists");
            await Task.CompletedTask;
            
            // For now, always return true
            // In a real implementation, this would check if the file exists
            return true;
        }

        /// <summary>
        /// Reloads the AI configuration, potentially refreshing it from the source such as a file or database.
        /// </summary>
        /// <returns>A task that represents the asynchronous reload operation. The task result contains the reloaded <see cref="AiConfiguration"/>.</returns>
        public async Task<AiConfiguration> ReloadAsync()
        {
            _logger.LogInformation("Reloading AI configuration");
            await Task.CompletedTask;
            
            // For now, return the current configuration
            // In a real implementation, this would reload from file/database
            return _configuration;
        }
    }
} 