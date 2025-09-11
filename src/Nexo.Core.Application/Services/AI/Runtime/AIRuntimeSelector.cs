using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
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
    /// Service for selecting the best AI runtime for operations
    /// </summary>
    public class AIRuntimeSelector : IAIRuntimeSelector
    {
        private readonly ILogger<AIRuntimeSelector> _logger;
        private readonly List<IAIProvider> _providers;
        private readonly Dictionary<AIProviderType, IAIProvider> _providerMap;

        public AIRuntimeSelector(ILogger<AIRuntimeSelector> logger)
        {
            _logger = logger;
            _providers = new List<IAIProvider>();
            _providerMap = new Dictionary<AIProviderType, IAIProvider>();
        }

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
        /// Gets all available AI providers
        /// </summary>
        public async Task<List<IAIProvider>> GetAvailableProvidersAsync()
        {
            var availableProviders = new List<IAIProvider>();

            foreach (var provider in _providers)
            {
                try
                {
                    if (provider.IsAvailable())
                    {
                        availableProviders.Add(provider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} failed availability check", provider.ProviderType);
                }
            }

            return availableProviders.OrderByDescending(p => p.Priority).ToList();
        }

        /// <summary>
        /// Gets providers that support the specified platform
        /// </summary>
        public async Task<List<IAIProvider>> GetProvidersForPlatformAsync(PlatformType platform)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            return availableProviders
                .Where(p => p.SupportsPlatform(platform))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        /// <summary>
        /// Gets providers that meet the specified requirements
        /// </summary>
        public async Task<List<IAIProvider>> GetProvidersForRequirementsAsync(AIRequirements requirements)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            return availableProviders
                .Where(p => p.MeetsRequirements(requirements))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        /// <summary>
        /// Registers a new AI provider
        /// </summary>
        public void RegisterProvider(IAIProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _logger.LogInformation("Registering AI provider: {ProviderType}", provider.ProviderType);

            _providers.Add(provider);
            _providerMap[provider.ProviderType] = provider;

            // Sort by priority
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Unregisters an AI provider
        /// </summary>
        public void UnregisterProvider(AIProviderType providerType)
        {
            _logger.LogInformation("Unregistering AI provider: {ProviderType}", providerType);

            var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
            if (provider != null)
            {
                _providers.Remove(provider);
                _providerMap.Remove(providerType);
            }
        }

        /// <summary>
        /// Gets the best provider for the given context
        /// </summary>
        public async Task<IAIProvider> GetBestProviderAsync(AIOperationContext context)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            
            if (!availableProviders.Any())
            {
                return null;
            }

            // Filter by platform support
            var platformProviders = availableProviders
                .Where(p => p.SupportsPlatform(context.Platform))
                .ToList();

            if (!platformProviders.Any())
            {
                _logger.LogWarning("No providers support platform: {Platform}", context.Platform);
                return null;
            }

            // Filter by requirements
            var requirementProviders = platformProviders
                .Where(p => p.MeetsRequirements(context.Requirements))
                .ToList();

            if (!requirementProviders.Any())
            {
                _logger.LogWarning("No providers meet requirements for operation: {OperationType}", context.OperationType);
                return null;
            }

            // Filter by resource availability
            var resourceProviders = requirementProviders
                .Where(p => p.HasRequiredResources(context.Resources))
                .ToList();

            if (!resourceProviders.Any())
            {
                _logger.LogWarning("No providers have required resources for operation: {OperationType}", context.OperationType);
                return null;
            }

            // Select best provider based on score
            var bestProvider = resourceProviders
                .OrderByDescending(p => CalculateProviderScore(p, context))
                .FirstOrDefault();

            return bestProvider;
        }

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

        #region Private Methods

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

        #endregion

        #region Additional Interface Methods

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
                Platform = context.ContainsKey("Platform") ? context["Platform"].ToString() : "Unknown",
                MaxTokens = context.ContainsKey("MaxTokens") ? Convert.ToInt32(context["MaxTokens"]) : 1000,
                Temperature = context.ContainsKey("Temperature") ? Convert.ToDouble(context["Temperature"]) : 0.7,
                Priority = context.ContainsKey("Priority") ? (AIPriority)context["Priority"] : AIPriority.Balanced
            };

            return await provider.CreateEngineAsync(contextObj);
        }

        /// <summary>
        /// Gets all available AI engines
        /// </summary>
        public async Task<List<AIEngineInfo>> GetAvailableEnginesAsync()
        {
            var engines = new List<AIEngineInfo>();
            var providers = await GetAvailableProvidersAsync();

            foreach (var provider in providers)
            {
                try
                {
                    var providerEngines = await provider.GetAvailableModelsAsync();
                    engines.AddRange(providerEngines);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get engines from provider: {ProviderType}", provider.ProviderType);
                }
            }

            return engines;
        }

        /// <summary>
        /// Gets information about a specific engine type
        /// </summary>
        public async Task<AIEngineInfo> GetEngineInfoAsync(AIEngineType engineType)
        {
            var engines = await GetAvailableEnginesAsync();
            return engines.FirstOrDefault(e => e.EngineType == engineType) ?? 
                   new AIEngineInfo { EngineType = engineType, Name = "Unknown", IsAvailable = false };
        }

        /// <summary>
        /// Checks if an engine type is available
        /// </summary>
        public async Task<bool> IsEngineAvailableAsync(AIEngineType engineType)
        {
            try
            {
                var engineInfo = await GetEngineInfoAsync(engineType);
                return engineInfo.IsAvailable;
            }
            catch
            {
                return false;
            }
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

        #endregion
    }

    /// <summary>
    /// Exception thrown when no AI provider is available
    /// </summary>
    public class NoAIProviderAvailableException : Exception
    {
        public NoAIProviderAvailableException(string message) : base(message) { }
        public NoAIProviderAvailableException(string message, Exception innerException) : base(message, innerException) { }
    }
}
