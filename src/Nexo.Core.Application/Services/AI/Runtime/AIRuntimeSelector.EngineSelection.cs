using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Engine selection functionality for AI runtime selection.
    /// </summary>
    public partial class AIRuntimeSelector
    {
        /// <summary>
        /// Selects the optimal AI provider for the given engine type and context
        /// </summary>
        public async Task<IAIProvider> SelectOptimalProviderAsync(AIEngineType engineType, Dictionary<string, object> context)
        {
            _logger.LogDebug("Selecting optimal AI provider for engine type: {EngineType}", engineType);

            var availableProviders = await GetAvailableProvidersAsync();
            var suitableProviders = availableProviders.Where(p => p.SupportsEngineType(engineType)).ToList();

            if (!suitableProviders.Any())
            {
                throw new NoAIProviderAvailableException($"No AI provider available for engine type {engineType}");
            }

            // Select the first suitable provider (can be enhanced with more sophisticated selection logic)
            var selectedProvider = suitableProviders.First();
            _logger.LogInformation("Selected AI provider: {ProviderType} for engine type: {EngineType}", 
                selectedProvider.ProviderType, engineType);

            return selectedProvider;
        }

        /// <summary>
        /// Selects the optimal AI engine for the given engine type and context
        /// </summary>
        public async Task<IAIEngine> SelectOptimalEngineAsync(AIEngineType engineType, Dictionary<string, object> context)
        {
            _logger.LogDebug("Selecting optimal AI engine for engine type: {EngineType}", engineType);

            var provider = await SelectOptimalProviderAsync(engineType, context);
            var contextObj = new AIOperationContext
            {
                OperationType = context.ContainsKey("OperationType") ? Enum.TryParse<AIOperationType>(context["OperationType"].ToString(), out var opType) ? opType : AIOperationType.CodeGeneration : AIOperationType.CodeGeneration,
                Platform = context.ContainsKey("Platform") ? Enum.TryParse<Nexo.Core.Domain.Enums.PlatformType>(context["Platform"].ToString(), out var platformType) ? platformType : Nexo.Core.Domain.Enums.PlatformType.Unknown : Nexo.Core.Domain.Enums.PlatformType.Unknown,
                MaxTokens = context.ContainsKey("MaxTokens") ? Convert.ToInt32(context["MaxTokens"]) : 1000,
                Temperature = context.ContainsKey("Temperature") ? Convert.ToDouble(context["Temperature"]) : 0.7,
                Priority = context.ContainsKey("Priority") ? context["Priority"]?.ToString() ?? AIPriority.Balanced.ToString() : AIPriority.Balanced.ToString()
            };

            return await provider.CreateEngineAsync(contextObj);
        }

        /// <summary>
        /// Selects the optimal AI provider for the given operation context
        /// </summary>
        public async Task<IAIProvider> SelectOptimalProviderAsync(AIOperationContext context)
        {
            _logger.LogDebug("Selecting optimal AI provider for operation: {OperationType}", context.OperationType);

            var availableProviders = await GetAvailableProvidersAsync();
            var suitableProviders = availableProviders.Where(p => p.SupportsPlatform(context.Platform)).ToList();

            if (!suitableProviders.Any())
            {
                throw new NoAIProviderAvailableException($"No AI provider available for operation {context.OperationType} on platform {context.Platform}");
            }

            // Select the first suitable provider (can be enhanced with more sophisticated selection logic)
            var selectedProvider = suitableProviders.First();
            _logger.LogInformation("Selected AI provider: {ProviderType} for operation: {OperationType}", 
                selectedProvider.ProviderType, context.OperationType);

            return selectedProvider;
        }
    }
}
