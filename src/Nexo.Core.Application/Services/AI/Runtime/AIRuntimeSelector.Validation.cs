using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Validation and performance functionality for AI runtime selection.
    /// </summary>
    public partial class AIRuntimeSelector
    {
        /// <summary>
        /// Validates that an operation can be performed
        /// </summary>
        public async Task<bool> CanPerformOperationAsync(AIOperationContext context)
        {
            try
            {
                var bestProvider = await GetBestProviderAsync(context);
                return bestProvider != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating operation: {OperationType}", context.OperationType);
                return false;
            }
        }

        /// <summary>
        /// Gets performance estimates for all available providers
        /// </summary>
        public async Task<Dictionary<AIProviderType, Nexo.Core.Domain.Results.PerformanceEstimate>> GetPerformanceEstimatesAsync(AIOperationContext context)
        {
            var estimates = new Dictionary<AIProviderType, Nexo.Core.Domain.Results.PerformanceEstimate>();
            var availableProviders = await GetAvailableProvidersAsync();

            foreach (var provider in availableProviders)
            {
                try
                {
                    var estimate = await provider.EstimatePerformanceAsync(context);
                    estimates[provider.ProviderType] = estimate;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get performance estimate for provider: {ProviderType}", provider.ProviderType);
                }
            }

            return estimates;
        }
    }
}
