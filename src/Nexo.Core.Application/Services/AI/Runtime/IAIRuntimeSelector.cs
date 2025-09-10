using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Interface for selecting the best AI runtime for operations
    /// </summary>
    public interface IAIRuntimeSelector
    {
        /// <summary>
        /// Selects the best AI engine for the given context
        /// </summary>
        Task<IAIEngine> SelectBestEngineAsync(AIOperationContext context);

        /// <summary>
        /// Gets all available AI providers
        /// </summary>
        Task<List<IAIProvider>> GetAvailableProvidersAsync();

        /// <summary>
        /// Gets providers that support the specified platform
        /// </summary>
        Task<List<IAIProvider>> GetProvidersForPlatformAsync(PlatformType platform);

        /// <summary>
        /// Gets providers that meet the specified requirements
        /// </summary>
        Task<List<IAIProvider>> GetProvidersForRequirementsAsync(AIRequirements requirements);

        /// <summary>
        /// Registers a new AI provider
        /// </summary>
        void RegisterProvider(IAIProvider provider);

        /// <summary>
        /// Unregisters an AI provider
        /// </summary>
        void UnregisterProvider(AIProviderType providerType);

        /// <summary>
        /// Gets the best provider for the given context
        /// </summary>
        Task<IAIProvider> GetBestProviderAsync(AIOperationContext context);

        /// <summary>
        /// Validates that an operation can be performed
        /// </summary>
        Task<bool> CanPerformOperationAsync(AIOperationContext context);

        /// <summary>
        /// Gets performance estimates for all available providers
        /// </summary>
        Task<Dictionary<AIProviderType, PerformanceEstimate>> GetPerformanceEstimatesAsync(AIOperationContext context);
    }
}
