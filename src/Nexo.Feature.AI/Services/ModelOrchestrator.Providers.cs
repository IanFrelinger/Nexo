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
/// Provider management functionality for ModelOrchestrator.
/// </summary>
public partial class ModelOrchestrator
{
    /// <summary>
    /// Registers a new model provider.
    /// </summary>
    private Task RegisterProviderInternalAsync(IModelProvider provider, CancellationToken cancellationToken)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        _providers.Add(provider);
        _logger.LogInformation("Registered model provider: {ProviderName}", provider.DisplayName);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the best model provider for a specific task.
    /// </summary>
    private Task<IModelProvider?> GetBestModelForTaskInternalAsync(string task, Enums.ModelType modelType, CancellationToken cancellationToken)
    {
        var suitableProviders = _providers
            .Where(p => p.SupportedModelTypes.Contains(modelType))
            .ToList();

        if (!suitableProviders.Any())
        {
            _logger.LogWarning("No providers found for task: {Task} with model type: {ModelType}", task, modelType);
            return Task.FromResult<IModelProvider?>(null);
        }

        // Simple selection logic - in a real implementation, this would be more sophisticated
        var bestProvider = suitableProviders.First();
        _logger.LogDebug("Selected provider {ProviderName} for task: {Task}", bestProvider.DisplayName, task);
        
        return Task.FromResult<IModelProvider?>(bestProvider);
    }
}
