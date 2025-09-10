using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;

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
        public async Task<Dictionary<AIProviderType, PerformanceEstimate>> GetPerformanceEstimatesAsync(AIOperationContext context)
        {
            var estimates = new Dictionary<AIProviderType, PerformanceEstimate>();
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
