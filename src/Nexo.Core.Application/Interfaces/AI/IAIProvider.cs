using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.AI
{
    /// <summary>
    /// Interface for AI providers that can create AI engines
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Gets the provider type
        /// </summary>
        AIProviderType ProviderType { get; }

        /// <summary>
        /// Gets the provider priority (higher = preferred)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the provider capabilities
        /// </summary>
        AIProviderCapabilities Capabilities { get; }

        /// <summary>
        /// Gets the current provider status
        /// </summary>
        AIProviderStatus Status { get; }

        /// <summary>
        /// Checks if the provider is available on the current platform
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Checks if the provider supports the specified platform
        /// </summary>
        bool SupportsPlatform(PlatformType platform);

        /// <summary>
        /// Checks if the provider meets the specified requirements
        /// </summary>
        bool MeetsRequirements(AIRequirements requirements);

        /// <summary>
        /// Checks if the provider has the required resources
        /// </summary>
        bool HasRequiredResources(AIResources resources);

        /// <summary>
        /// Initializes the provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Creates an AI engine instance
        /// </summary>
        Task<IAIEngine> CreateEngineAsync(AIOperationContext context);

        /// <summary>
        /// Gets available models for this provider
        /// </summary>
        Task<List<ModelInfo>> GetAvailableModelsAsync();

        /// <summary>
        /// Downloads a model for this provider
        /// </summary>
        Task<ModelInfo> DownloadModelAsync(string modelId, string variantId);

        /// <summary>
        /// Validates that a model is compatible with this provider
        /// </summary>
        bool IsModelCompatible(ModelInfo model);

        /// <summary>
        /// Gets the estimated performance for an operation
        /// </summary>
        Task<Nexo.Core.Domain.Results.PerformanceEstimate> EstimatePerformanceAsync(AIOperationContext context);
    }
}
