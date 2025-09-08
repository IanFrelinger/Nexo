using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using static Nexo.Feature.AI.Models.HealthStatus;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// Model orchestrator implementation that manages multiple AI model providers.
/// </summary>
public class ModelOrchestrator : IModelOrchestrator
{
    private readonly ILogger<ModelOrchestrator> _logger;
    private readonly Dictionary<string, IModelProvider> _providers;
    private readonly Dictionary<string, IModel> _loadedModels;
    private readonly object _lock = new object();

    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = new Dictionary<string, IModelProvider>();
        _loadedModels = new Dictionary<string, IModel>();
    }

    // IModelOrchestrator interface implementation
    public IEnumerable<IModelProvider> Providers
    {
        get
        {
            lock (_lock)
            {
                return _providers.Values.ToList();
            }
        }
    }

    public void RegisterProvider(IModelProvider provider)
    {
        if (provider != null)
            lock (_lock)
            {
                _providers[provider.ProviderId] = provider;
                _logger.LogInformation("Registered model provider: {ProviderId}", provider.ProviderId);
            }
        else
            throw new ArgumentNullException(nameof(provider));
    }

    public void UnregisterProvider(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));

        lock (_lock)
        {
            if (_providers.Remove(providerId))
            {
                _logger.LogInformation("Unregistered model provider: {ProviderId}", providerId);
            }
        }
    }

    public IModelProvider? GetProvider(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
            return null;

        lock (_lock)
        {
            return _providers.GetValueOrDefault(providerId);
        }
    }

    public async Task<IEnumerable<ModelInfo>> GetAllAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        var allModels = new List<ModelInfo>();

        foreach (var provider in Providers)
        {
            try
            {
                var models = await provider.GetAvailableModelsAsync(cancellationToken);
                allModels.AddRange(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models from provider {ProviderId}", provider.ProviderId);
            }
        }

        return allModels;
    }

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(ModelType modelType, CancellationToken cancellationToken = default(CancellationToken))
    {
        var allModels = await GetAllAvailableModelsAsync(cancellationToken);
        return allModels.Where(m => m.ModelType == modelType);
    }

    public async Task<IModel> LoadModelAsync(string modelName, string preferredProvider, CancellationToken cancellationToken = default(CancellationToken))
    {
        // Try preferred provider first
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            var provider = GetProvider(preferredProvider);
            if (provider != null)
            {
                try
                {
                    var model = await provider.LoadModelAsync(modelName, cancellationToken);
                    lock (_lock)
                    {
                        _loadedModels[modelName] = model;
                    }
                    return model;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load model {ModelName} from preferred provider {ProviderId}", modelName, preferredProvider);
                }
            }
        }

        // Try all providers
        foreach (var provider in Providers)
        {
            try
            {
                var model = await provider.LoadModelAsync(modelName, cancellationToken);
                lock (_lock)
                {
                    _loadedModels[modelName] = model;
                }
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load model {ModelName} from provider {ProviderId}", modelName, provider.ProviderId);
            }
        }

        throw new InvalidOperationException($"Model {modelName} could not be loaded from any provider");
    }



    public Task<IEnumerable<IModelProvider>> GetProvidersAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(Providers);
    }

    public Task<IModelProvider?> SelectModelAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Selecting model for request");
        
        // Simple implementation: return the first available provider
        return Task.FromResult(Providers.FirstOrDefault());
    }

    public async Task<IEnumerable<ModelHealthStatus>> GetHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        var healthStatuses = new List<ModelHealthStatus>();

        foreach (var provider in Providers)
        {
            try
            {
                var healthStatus = await provider.GetHealthStatusAsync(cancellationToken);
                healthStatuses.Add(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status for provider {ProviderId}", provider.ProviderId);
                healthStatuses.Add(new ModelHealthStatus
                {
                    IsHealthy = false,
                    Status = $"Error: {ex.Message}",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = 0,
                    ErrorRate = 1.0
                });
            }
        }

        return healthStatuses;
    }

    public Task<ModelOptimizationResult> OptimizeAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Optimizing model orchestration");
        
        var result = new ModelOptimizationResult
        {
            IsSuccessful = true,
            ModelName = "orchestration",
            PerformanceImprovement = 0.0,
            MemoryReduction = 0.0,
            Recommendations = new List<string> { "Model orchestration optimized successfully" }
        };

        return Task.FromResult(result);
    }

    public async Task RegisterProviderAsync(IModelProvider provider, CancellationToken cancellationToken = default(CancellationToken))
    {
        RegisterProvider(provider);
        await Task.CompletedTask;
    }

    public async Task UnregisterProviderAsync(string providerName, CancellationToken cancellationToken = default(CancellationToken))
    {
        UnregisterProvider(providerName);
        await Task.CompletedTask;
    }

    public async Task<IModelProvider?> GetBestModelForTaskAsync(string task, ModelType modelType, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Getting best model provider for task: {Task}, type: {ModelType}", task, modelType);

        // Simple implementation: return the first available provider of the requested type
        var availableModels = await GetAvailableModelsAsync(modelType, cancellationToken);
        var firstModel = availableModels.FirstOrDefault();

        if (firstModel == null) return null;
        // Find the provider for this model
        foreach (var provider in Providers)
        {
            try
            {
                var models = await provider.GetAvailableModelsAsync(cancellationToken);
                if (models.Any(m => m.Name == firstModel.Name))
                {
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking models for provider {ProviderId}", provider.ProviderId);
            }
        }

        return null;
    }

    public async Task<IEnumerable<ProviderHealthStatus>> GetAllProviderHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        var healthStatuses = new List<ProviderHealthStatus>();

        foreach (var provider in Providers)
        {
            try
            {
                var healthStatus = await provider.GetHealthStatusAsync(cancellationToken);
                healthStatuses.Add(new ProviderHealthStatus
                {
                    ProviderName = provider.ProviderId,
                    Status = healthStatus.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                    LastChecked = healthStatus.LastChecked,
                    ResponseTimeMs = healthStatus.ResponseTimeMs,
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_rate"] = healthStatus.ErrorRate,
                        ["status"] = healthStatus.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status for provider {ProviderId}", provider.ProviderId);
                healthStatuses.Add(new ProviderHealthStatus
                {
                    ProviderName = provider.ProviderId,
                    Status = HealthStatus.Unhealthy,
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = 0,
                    ErrorMessage = ex.Message,
                    Metrics = new Dictionary<string, object>
                    {
                        ["error"] = ex.Message
                    }
                });
            }
        }

        return healthStatuses;
    }

    // Legacy methods for backward compatibility
    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Executing model request with orchestrator");

        // Try to find a suitable provider
        var provider = await GetBestModelForTaskAsync("text generation", ModelType.TextGeneration, cancellationToken);
        if (provider == null)
        {
            throw new InvalidOperationException("No suitable model provider available");
        }

        return await provider.ExecuteAsync(request, cancellationToken);
    }

    public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
    {
        _logger.LogInformation("Validating model request");

        var errors = new List<string>();

        // Basic validation
        if (string.IsNullOrEmpty(request.Input))
        {
            errors.Add("Input is required");
        }

        // Check if any provider is available
        var healthyProviders = await GetHealthyProvidersAsync();
        if (!healthyProviders.Any())
        {
            errors.Add("No healthy model providers available");
        }

        return new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private async Task<IEnumerable<IModelProvider>> GetHealthyProvidersAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        var healthyProviders = new List<IModelProvider>();

        foreach (var provider in Providers)
        {
            try
            {
                var healthStatus = await provider.GetHealthStatusAsync(cancellationToken);
                if (healthStatus.IsHealthy)
                {
                    healthyProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking health for provider {ProviderId}", provider.ProviderId);
            }
        }

        return healthyProviders;
    }

    // Additional utility methods
    public async Task<ModelOptimizationResult> OptimizeModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Optimizing model: {ModelName}", modelName);

        var result = new ModelOptimizationResult
        {
            IsSuccessful = true,
            ModelName = modelName,
            PerformanceImprovement = 0.0,
            MemoryReduction = 0.0,
            Recommendations = new List<string> { $"Model {modelName} optimized successfully" }
        };

        // Placeholder for optimization logic
        await Task.Delay(100, cancellationToken); // Simulate optimization time

        return result;
    }

    public async Task<ProviderHealthStatus> GetProviderHealthStatusAsync(string providerId, CancellationToken cancellationToken = default(CancellationToken))
    {
        var provider = GetProvider(providerId);
        if (provider == null)
        {
            return new ProviderHealthStatus
            {
                ProviderName = providerId,
                Status = HealthStatus.Unhealthy,
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = 0,
                ErrorMessage = "Provider not found",
                Metrics = new Dictionary<string, object>
                {
                    ["error"] = "Provider not found"
                }
            };
        }

        var healthStatus = await provider.GetHealthStatusAsync(cancellationToken);
        return new ProviderHealthStatus
        {
            ProviderName = providerId,
            Status = healthStatus.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            LastChecked = healthStatus.LastChecked,
            ResponseTimeMs = healthStatus.ResponseTimeMs,
            Metrics = new Dictionary<string, object>
            {
                ["error_rate"] = healthStatus.ErrorRate,
                ["status"] = healthStatus.Status
            }
        };
    }

    public void ClearLoadedModels()
    {
        lock (_lock)
        {
            _loadedModels.Clear();
            _logger.LogInformation("Cleared all loaded models");
        }
    }

    public IModel? GetLoadedModel(string modelName)
    {
        lock (_lock)
        {
            return _loadedModels.GetValueOrDefault(modelName);
        }
    }

    public IEnumerable<string> GetLoadedModelNames()
    {
        lock (_lock)
        {
            return _loadedModels.Keys.ToList();
        }
    }
}
}