using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Service for managing AI configuration settings.
    /// </summary>
    public class AIConfigurationService : IAIConfigurationService
    {
        private readonly ILogger<AIConfigurationService> _logger;
        private AIConfiguration _configuration;

        public AIConfigurationService(ILogger<AIConfigurationService> logger)
        {
            _logger = logger;
            _configuration = GetDefaultConfiguration(AIMode.Development);
        }

        public async Task<AIConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting AI configuration");
            await Task.CompletedTask;
            return _configuration;
        }

        public async Task SaveConfigurationAsync(AIConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            _logger.LogInformation("Saving AI configuration");
            await Task.CompletedTask;
            
            _configuration = configuration;
        }

        public async Task<AIConfiguration> LoadForModeAsync(AIMode mode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading AI configuration for mode: {Mode}", mode);
            await Task.CompletedTask;
            
            var config = GetDefaultConfiguration(mode);
            await SaveConfigurationAsync(config, cancellationToken);
            return config;
        }

        public AIConfiguration GetDefaultConfiguration(AIMode mode)
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
                case AIMode.Development:
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
                case AIMode.Production:
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
                case AIMode.AIHeavy:
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

            return new AIConfiguration
            {
                Mode = mode,
                Model = new AIModelConfiguration
                {
                    Name = modelName,
                    MaxInputTokens = maxInputTokens,
                    MaxOutputTokens = maxOutputTokens,
                    Temperature = temperature,
                    EnableStreaming = true,
                    RequestTimeoutSeconds = requestTimeoutSeconds
                },
                Resources = new AIResourceConfiguration
                {
                    MaxConcurrentRequests = maxConcurrentRequests,
                    MaxMemoryUsageBytes = maxMemoryUsageBytes,
                    MaxCpuUsagePercentage = maxCpuUsagePercentage,
                    MaxGpuMemoryUsageBytes = maxGpuMemoryUsageBytes,
                    MaxGpuUsagePercentage = maxGpuUsagePercentage,
                    EnableResourceMonitoring = true,
                    ResourceMonitoringIntervalSeconds = 30
                },
                Performance = new AIPerformanceConfiguration
                {
                    Mode = perfMode,
                    TargetResponseTimeMs = targetResponseTimeMs,
                    MaxResponseTimeMs = maxResponseTimeMs
                },
                Caching = new AICachingConfiguration
                {
                    Enabled = mode != AIMode.Development,
                    MaxCacheSizeBytes = maxCacheSizeBytes,
                    DefaultExpirationSeconds = defaultExpirationSeconds,
                    EvictionPolicy = CacheEvictionPolicy.LeastRecentlyUsed,
                    EnableCompression = mode != AIMode.Development,
                    CompressionLevel = 6
                },
                Fallback = new AIFallbackConfiguration
                {
                    Enabled = mode != AIMode.Development,
                    MaxFallbackAttempts = maxFallbackAttempts,
                    FallbackDelaySeconds = fallbackDelaySeconds,
                    EnableExponentialBackoff = mode != AIMode.Development,
                    EnableOfflineMode = mode != AIMode.Development,
                    EnableCachedResponseFallback = mode != AIMode.Development
                },
                Monitoring = new AIMonitoringConfiguration
                {
                    Enabled = mode != AIMode.Development,
                    CollectionIntervalSeconds = collectionIntervalSeconds,
                    EnableRequestResponseLogging = true,
                    EnablePerformanceMetrics = mode != AIMode.Development,
                    EnableErrorTracking = mode != AIMode.Development,
                    EnableUsageAnalytics = mode != AIMode.Development,
                    EnableHealthChecks = mode != AIMode.Development,
                    AlertThresholds = new AIAlertThresholds
                    {
                        MaxResponseTimeMs = maxResponseTimeAlert,
                        MaxErrorRate = maxErrorRateAlert
                    }
                }
            };
        }

        public async Task<AIConfigurationValidationResult> ValidateAsync(AIConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            
            _logger.LogInformation("Validating AI configuration");
            await Task.CompletedTask;
            
            var result = new AIConfigurationValidationResult { IsValid = true };
            
            // Validate model configuration
            if (configuration.Model != null)
            {
                if (string.IsNullOrEmpty(configuration.Model.Name))
                {
                    result.Errors.Add(new AIConfigurationValidationError
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
                    result.Errors.Add(new AIConfigurationValidationError
                    {
                        Code = "MAX_INPUT_TOKENS_INVALID",
                        Message = "Max input tokens must be greater than 0",
                        FieldPath = "model.maxInputTokens",
                        Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
                
                if (configuration.Model.MaxOutputTokens <= 0)
                {
                    result.Errors.Add(new AIConfigurationValidationError
                    {
                        Code = "MAX_OUTPUT_TOKENS_INVALID",
                        Message = "Max output tokens must be greater than 0",
                        FieldPath = "model.maxOutputTokens",
                        Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
                    });
                    result.IsValid = false;
                }
            }
            else
            {
                result.Errors.Add(new AIConfigurationValidationError
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
                    result.Errors.Add(new AIConfigurationValidationError
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
                    result.Errors.Add(new AIConfigurationValidationError
                    {
                        Code = "MAX_MEMORY_USAGE_INVALID",
                        Message = "Max memory usage must be greater than 0",
                        FieldPath = "resources.maxMemoryUsageBytes",
                        Severity = Nexo.Feature.AI.Interfaces.ValidationSeverity.Error
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

        public async Task<AIConfiguration> MergeAsync(IEnumerable<AIConfiguration> configurations, CancellationToken cancellationToken = default)
        {
            if (configurations == null)
            {
                throw new ArgumentNullException(nameof(configurations));
            }
            
            _logger.LogInformation("Merging AI configurations");
            await Task.CompletedTask;
            
            var configList = configurations.ToList();
            if (!configList.Any())
            {
                return GetDefaultConfiguration(AIMode.Development);
            }
            
            // For now, return the last configuration
            // In a real implementation, this would merge all configurations
            return configList.Last();
        }

        public string GetConfigurationPath()
        {
            return "ai-config.json";
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Checking if AI configuration exists");
            await Task.CompletedTask;
            
            // For now, always return true
            // In a real implementation, this would check if the file exists
            return true;
        }

        public async Task<AIConfiguration> ReloadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reloading AI configuration");
            await Task.CompletedTask;
            
            // For now, return the current configuration
            // In a real implementation, this would reload from file/database
            return _configuration;
        }
    }
} 