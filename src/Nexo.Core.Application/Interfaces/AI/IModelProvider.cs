using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Interface for AI model providers
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// Gets the provider type
        /// </summary>
        AIProviderType ProviderType { get; }

        /// <summary>
        /// Gets the provider name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the provider version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets whether the provider is available
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Gets the available models
        /// </summary>
        Task<List<ModelInfo>> GetAvailableModelsAsync();

        /// <summary>
        /// Downloads a model
        /// </summary>
        Task<bool> DownloadModelAsync(string modelId, string variant = null);

        /// <summary>
        /// Checks if a model is compatible
        /// </summary>
        bool IsModelCompatible(ModelInfo model);

        /// <summary>
        /// Gets model information
        /// </summary>
        Task<ModelInfo> GetModelInfoAsync(string modelId);
    }
}
