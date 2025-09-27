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
/// Model execution functionality for ModelOrchestrator.
/// </summary>
public partial class ModelOrchestrator
{
    /// <summary>
    /// Executes a request using the best available model.
    /// </summary>
    private async Task<ModelResponse> ExecuteRequestAsync(ModelRequest request, CancellationToken cancellationToken)
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

    /// <summary>
    /// Gets the best provider for a specific request.
    /// </summary>
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
