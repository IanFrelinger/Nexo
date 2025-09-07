using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Orchestrates AI model operations and provider management
/// </summary>
public class ModelOrchestrator : IModelOrchestrator
{
    private readonly List<IModelProvider> _providers;
    private readonly ILogger<ModelOrchestrator> _logger;

    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _providers = new List<IModelProvider>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var bestProvider = await GetBestProviderForRequest(request, cancellationToken);
            if (bestProvider == null)
            {
                return new ModelResponse
                {
                    Success = false,
                    ErrorMessage = "No suitable model provider available",
                    Response = string.Empty
                };
            }

            return await bestProvider.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing model request");
            return new ModelResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Response = string.Empty
            };
        }
    }

    public Task RegisterProviderAsync(IModelProvider provider, CancellationToken cancellationToken = default)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        _providers.Add(provider);
        _logger.LogInformation("Registered model provider: {ProviderName}", provider.DisplayName);
        
        return Task.CompletedTask;
    }

    public Task<IModelProvider> GetBestModelForTaskAsync(string task, Enums.ModelType modelType, CancellationToken cancellationToken = default)
    {
        var suitableProviders = _providers
            .Where(p => p.SupportedModelTypes.Contains(modelType))
            .ToList();

        if (!suitableProviders.Any())
        {
            _logger.LogWarning("No providers found for task: {Task} with model type: {ModelType}", task, modelType);
            return Task.FromResult<IModelProvider>(null!);
        }

        // Simple selection logic - in a real implementation, this would be more sophisticated
        var bestProvider = suitableProviders.First();
        _logger.LogDebug("Selected provider {ProviderName} for task: {Task}", bestProvider.DisplayName, task);
        
        return Task.FromResult(bestProvider);
    }

    private async Task<IModelProvider?> GetBestProviderForRequest(ModelRequest request, CancellationToken cancellationToken)
    {
        if (!_providers.Any())
        {
            _logger.LogWarning("No model providers registered");
            return null;
        }

        // Simple selection logic - use the first available provider
        // In a real implementation, this would consider:
        // - Provider health status
        // - Request complexity
        // - Provider capabilities
        // - Load balancing
        
        foreach (var provider in _providers)
        {
            try
            {
                var healthStatus = await provider.GetHealthStatusAsync(cancellationToken);
                if (healthStatus.IsHealthy)
                {
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check health status for provider: {ProviderName}", provider.DisplayName);
            }
        }

        _logger.LogWarning("No healthy providers available");
        return null;
    }
}
