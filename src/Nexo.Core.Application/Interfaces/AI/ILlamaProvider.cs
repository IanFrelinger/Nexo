using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Application.Services.AI.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Interfaces.AI
{
    /// <summary>
    /// Interface for LLama-based AI providers that support offline operation
    /// </summary>
    public interface ILlamaProvider : IModelProvider
    {
        /// <summary>
        /// Gets whether a specific model is currently loaded in memory
        /// </summary>
        bool IsModelLoaded(string modelName);

        /// <summary>
        /// Loads a model into memory for faster inference
        /// </summary>
        Task LoadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads a model from memory to free up resources
        /// </summary>
        Task UnloadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current memory usage of the loaded model
        /// </summary>
        Task<long> GetModelMemoryUsageAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether the provider supports GPU acceleration
        /// </summary>
        bool SupportsGpuAcceleration { get; }

        /// <summary>
        /// Gets whether the provider is capable of offline operation
        /// </summary>
        bool IsOfflineCapable { get; }

        /// <summary>
        /// Gets the priority of this provider (higher = preferred)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Downloads a model from the provider's repository
        /// </summary>
        Task<ModelInfo> DownloadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a downloaded model from local storage
        /// </summary>
        Task<bool> RemoveModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about available models for download
        /// </summary>
        Task<IEnumerable<ModelInfo>> GetAvailableModelsForDownloadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the local storage path for models
        /// </summary>
        string ModelsPath { get; }

        /// <summary>
        /// Gets the maximum context length supported by the provider
        /// </summary>
        int MaxContextLength { get; }

        /// <summary>
        /// Gets whether the provider supports streaming responses
        /// </summary>
        bool SupportsStreaming { get; }
    }
}
