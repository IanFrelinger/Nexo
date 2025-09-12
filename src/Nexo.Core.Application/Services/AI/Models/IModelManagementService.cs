using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Models
{
    /// <summary>
    /// Service for managing AI models across platforms
    /// </summary>
    public interface IModelManagementService
    {
        /// <summary>
        /// Downloads a model for the specified platform
        /// </summary>
        Task<ModelInfo> DownloadModelAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform, string? variantId = null);

        /// <summary>
        /// Gets the path to a model file
        /// </summary>
        Task<string> GetModelPathAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform);

        /// <summary>
        /// Checks if a model is available locally
        /// </summary>
        Task<bool> IsModelAvailableAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform);

        /// <summary>
        /// Gets information about a model
        /// </summary>
        Task<ModelInfo?> GetModelInfoAsync(string modelId);

        /// <summary>
        /// Lists all available models for a platform
        /// </summary>
        Task<List<ModelInfo>> ListModelsAsync(Nexo.Core.Domain.Enums.PlatformType platform);

        /// <summary>
        /// Lists all model variants for a model
        /// </summary>
        Task<List<ModelVariant>> ListModelVariantsAsync(string modelId);

        /// <summary>
        /// Caches a model for offline use
        /// </summary>
        Task CacheModelAsync(string modelId, Stream modelData, Nexo.Core.Domain.Enums.PlatformType platform);

        /// <summary>
        /// Removes a cached model
        /// </summary>
        Task RemoveModelAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform);

        /// <summary>
        /// Validates a model file
        /// </summary>
        Task<bool> ValidateModelAsync(string modelPath);

        /// <summary>
        /// Gets the best model variant for a platform and requirements
        /// </summary>
        Task<ModelVariant> GetBestModelVariantAsync(Nexo.Core.Domain.Enums.PlatformType platform, AIRequirements requirements);

        /// <summary>
        /// Preloads models for better performance
        /// </summary>
        Task PreloadModelsAsync(Nexo.Core.Domain.Enums.PlatformType platform, List<string> modelIds);

        /// <summary>
        /// Cleans up old or unused models
        /// </summary>
        Task CleanupModelsAsync(TimeSpan maxAge);

        /// <summary>
        /// Gets storage usage statistics
        /// </summary>
        Task<ModelStorageStatistics> GetStorageStatisticsAsync();
    }
}
