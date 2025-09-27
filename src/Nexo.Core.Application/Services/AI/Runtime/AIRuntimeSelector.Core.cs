using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Core functionality for AI runtime selection.
    /// </summary>
    public partial class AIRuntimeSelector
    {
        /// <summary>
        /// Selects the best AI engine for the given context
        /// </summary>
        public async Task<IAIEngine> SelectBestEngineAsync(AIOperationContext context)
        {
            _logger.LogDebug("Selecting best AI engine for operation: {OperationType}", context.OperationType);

            var bestProvider = await GetBestProviderAsync(context);
            if (bestProvider == null)
            {
                throw new NoAIProviderAvailableException($"No AI provider available for operation {context.OperationType} on platform {context.Platform}");
            }

            _logger.LogInformation("Selected AI provider: {ProviderType} for operation: {OperationType}", 
                bestProvider.ProviderType, context.OperationType);

            return await bestProvider.CreateEngineAsync(context);
        }

        /// <summary>
        /// Gets the best provider for the given context
        /// </summary>
        public async Task<IAIProvider> GetBestProviderAsync(AIOperationContext context)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            
            if (!availableProviders.Any())
            {
                throw new NoAIProviderAvailableException("No AI providers available");
            }

            // Filter by platform support
            var platformProviders = availableProviders
                .Where(p => p.SupportsPlatform(context.Platform))
                .ToList();

            if (!platformProviders.Any())
            {
                _logger.LogWarning("No providers support platform: {Platform}", context.Platform);
                throw new NoAIProviderAvailableException($"No AI providers available for platform {context.Platform}");
            }

            // Filter by requirements
            var requirementProviders = platformProviders
                .Where(p => p.MeetsRequirements(context.Requirements))
                .ToList();

            if (!requirementProviders.Any())
            {
                _logger.LogWarning("No providers meet requirements for operation: {OperationType}", context.OperationType);
                throw new NoAIProviderAvailableException($"No AI providers meet requirements for operation {context.OperationType}");
            }

            // Filter by resource availability
            var resourceProviders = requirementProviders
                .Where(p => p.HasRequiredResources(context.Resources))
                .ToList();

            if (!resourceProviders.Any())
            {
                _logger.LogWarning("No providers have required resources for operation: {OperationType}", context.OperationType);
                throw new NoAIProviderAvailableException($"No AI providers have required resources for operation {context.OperationType}");
            }

            // Select best provider based on score
            var bestProvider = resourceProviders
                .OrderByDescending(p => CalculateProviderScore(p, context))
                .FirstOrDefault();

            if (bestProvider == null)
            {
                throw new NoAIProviderAvailableException("No suitable AI provider found");
            }

            return bestProvider;
        }

        /// <summary>
        /// Calculates a score for a provider based on context
        /// </summary>
        private int CalculateProviderScore(IAIProvider provider, AIOperationContext context)
        {
            var score = 0;

            // Base priority score
            score += provider.Priority * 10;

            // Platform compatibility bonus
            if (provider.SupportsPlatform(context.Platform))
                score += 50;

            // Requirements satisfaction bonus
            if (provider.MeetsRequirements(context.Requirements))
                score += 30;

            // Resource availability bonus
            if (provider.HasRequiredResources(context.Resources))
                score += 20;

            // Offline capability bonus (if required)
            if (context.Requirements.RequiresOffline && provider.Capabilities.SupportsOfflineMode)
                score += 25;

            // Operation type support bonus
            if (provider.Capabilities.SupportedOperations.Contains(context.OperationType))
                score += 15;

            // Streaming support bonus (if beneficial)
            if (provider.Capabilities.SupportsStreaming && 
                (context.OperationType == AIOperationType.CodeGeneration || 
                 context.OperationType == AIOperationType.Documentation))
                score += 10;

            // Batch processing bonus (if beneficial)
            if (provider.Capabilities.SupportsBatchProcessing && 
                context.Parameters.ContainsKey("BatchSize") && 
                (int)context.Parameters["BatchSize"] > 1)
                score += 5;

            return score;
        }
    }
}
