using Microsoft.Extensions.Logging;
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
    public class ModelManagementService : IModelManagementService
    {
        private readonly ILogger<ModelManagementService> _logger;
        private readonly string _modelCachePath;
        private readonly Dictionary<string, ModelInfo> _cachedModels;

        public ModelManagementService(ILogger<ModelManagementService> logger)
        {
            _logger = logger;
            _modelCachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Nexo", "Models");
            _cachedModels = new Dictionary<string, ModelInfo>();
            
            // Ensure model cache directory exists
            Directory.CreateDirectory(_modelCachePath);
        }

        public async Task<ModelInfo> DownloadModelAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform, string? variantId = null)
        {
            _logger.LogInformation("Downloading model {ModelId} for platform {Platform}", modelId, platform);

            // For now, return a mock model
            // In a real implementation, this would download from a model repository
            var model = new ModelInfo
            {
                Id = modelId,
                Name = $"Mock {modelId}",
                Description = $"Mock model {modelId} for {platform}",
                EngineType = AIEngineType.CodeLlama,
                Precision = ModelPrecision.Q4_0,
                SizeBytes = GetMockModelSize(platform),
                FilePath = Path.Combine(_modelCachePath, $"{modelId}-{platform}.gguf"),
                Checksum = Guid.NewGuid().ToString(),
                SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { platform },
                IsCached = true,
                CreatedAt = DateTime.UtcNow
            };

            // Simulate download delay
            await Task.Delay(1000);

            _cachedModels[modelId] = model;
            
            _logger.LogInformation("Model {ModelId} downloaded successfully", modelId);
            return model;
        }

        public async Task<string> GetModelPathAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            if (_cachedModels.TryGetValue(modelId, out var model))
            {
                return model.FilePath;
            }

            // Download model if not cached
            var downloadedModel = await DownloadModelAsync(modelId, platform);
            return downloadedModel.FilePath;
        }

        public Task<bool> IsModelAvailableAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            if (_cachedModels.TryGetValue(modelId, out var model))
            {
                return Task.FromResult(model.SupportedPlatforms.Contains(platform) && File.Exists(model.FilePath));
            }

            return Task.FromResult(false);
        }

        public Task<ModelInfo?> GetModelInfoAsync(string modelId)
        {
            if (_cachedModels.TryGetValue(modelId, out var model))
            {
                return Task.FromResult<ModelInfo?>(model);
            }

            return Task.FromResult<ModelInfo?>(null);
        }

        public Task<List<ModelInfo>> ListModelsAsync(Nexo.Core.Domain.Enums.PlatformType platform)
        {
            return Task.FromResult(_cachedModels.Values
                .Where(m => m.SupportedPlatforms.Contains(platform))
                .ToList());
        }

        public Task<List<ModelVariant>> ListModelVariantsAsync(string modelId)
        {
            // Return mock variants for different platforms
            var variants = new List<ModelVariant>();
            
            foreach (Nexo.Core.Domain.Enums.PlatformType platform in Enum.GetValues<Nexo.Core.Domain.Enums.PlatformType>())
            {
                variants.Add(new ModelVariant
                {
                    ModelId = modelId,
                    Name = $"{modelId}-{platform}",
                    Platform = platform,
                    Precision = ModelPrecision.Q4_0,
                    SizeBytes = GetMockModelSize(platform),
                    FileName = $"{modelId}-{platform}.gguf",
                    DownloadUrl = $"https://models.nexo.dev/{modelId}-{platform}.gguf",
                    Checksum = Guid.NewGuid().ToString()
                });
            }

            return Task.FromResult(variants);
        }

        public async Task CacheModelAsync(string modelId, Stream modelData, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            _logger.LogInformation("Caching model {ModelId} for platform {Platform}", modelId, platform);

            var filePath = Path.Combine(_modelCachePath, $"{modelId}-{platform}.gguf");
            
            using (var fileStream = File.Create(filePath))
            {
                await modelData.CopyToAsync(fileStream);
            }

            var model = new ModelInfo
            {
                Id = modelId,
                Name = $"Cached {modelId}",
                Description = $"Cached model {modelId} for {platform}",
                EngineType = AIEngineType.CodeLlama,
                Precision = ModelPrecision.Q4_0,
                SizeBytes = new FileInfo(filePath).Length,
                FilePath = filePath,
                Checksum = CalculateChecksum(filePath),
                SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { platform },
                IsCached = true,
                CreatedAt = DateTime.UtcNow
            };

            _cachedModels[modelId] = model;
            
            _logger.LogInformation("Model {ModelId} cached successfully", modelId);
        }

        public Task RemoveModelAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            _logger.LogInformation("Removing model {ModelId} for platform {Platform}", modelId, platform);

            if (_cachedModels.TryGetValue(modelId, out var model))
            {
                if (File.Exists(model.FilePath))
                {
                    File.Delete(model.FilePath);
                }
                
                _cachedModels.Remove(modelId);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> ValidateModelAsync(string modelPath)
        {
            if (!File.Exists(modelPath))
            {
                return Task.FromResult(false);
            }

            // Simple validation - check file size and extension
            var fileInfo = new FileInfo(modelPath);
            return Task.FromResult(fileInfo.Length > 0 && Path.GetExtension(fileInfo.Name).Equals(".gguf", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<ModelVariant> GetBestModelVariantAsync(Nexo.Core.Domain.Enums.PlatformType platform, AIRequirements requirements)
        {
            var variants = await ListModelVariantsAsync("default");
            var platformVariants = variants.Where(v => v.Platform == platform).ToList();

            if (!platformVariants.Any())
            {
                throw new InvalidOperationException($"No model variants available for platform {platform}");
            }

            // Select best variant based on requirements
            var bestVariant = platformVariants.First();
            
            if (requirements.RequiresOffline)
            {
                // Prefer smaller models for offline use
                bestVariant = platformVariants.OrderBy(v => v.SizeBytes).First();
            }
            else if (requirements.RequiresHighQuality)
            {
                // Prefer larger models for high quality
                bestVariant = platformVariants.OrderByDescending(v => v.SizeBytes).First();
            }

            return bestVariant;
        }

        public async Task PreloadModelsAsync(Nexo.Core.Domain.Enums.PlatformType platform, List<string> modelIds)
        {
            _logger.LogInformation("Preloading {Count} models for platform {Platform}", modelIds.Count, platform);

            var tasks = modelIds.Select(modelId => DownloadModelAsync(modelId, platform));
            await Task.WhenAll(tasks);
        }

        public async Task CleanupModelsAsync(TimeSpan maxAge)
        {
            _logger.LogInformation("Cleaning up models older than {MaxAge}", maxAge);

            var cutoffTime = DateTime.UtcNow - maxAge;
            var modelsToRemove = _cachedModels.Values
                .Where(m => m.CreatedAt < cutoffTime)
                .ToList();

            foreach (var model in modelsToRemove)
            {
                await RemoveModelAsync(model.Id, model.SupportedPlatforms.First());
            }
        }

        public Task<ModelStorageStatistics> GetStorageStatisticsAsync()
        {
            var totalSize = _cachedModels.Values.Sum(m => m.SizeBytes);
            var totalModels = _cachedModels.Count;
            var cachedModels = _cachedModels.Values.Count(m => m.IsCached);
            var availableSpace = GetAvailableDiskSpace(_modelCachePath);

            var platformStats = _cachedModels.Values
                .GroupBy(m => m.SupportedPlatforms.First())
                .ToDictionary(
                    g => g.Key,
                    g => new PlatformStorageStats
                    {
                        Platform = ConvertToInfrastructurePlatformType(g.Key),
                        SizeBytes = g.Sum(m => m.SizeBytes),
                        ModelCount = g.Count(),
                        AvailableSpaceBytes = availableSpace,
                        UsagePercentage = (double)g.Sum(m => m.SizeBytes) / (g.Sum(m => m.SizeBytes) + availableSpace) * 100
                    }
                );

            var precisionStats = _cachedModels.Values
                .GroupBy(m => m.Precision)
                .ToDictionary(
                    g => g.Key,
                    g => new PrecisionStorageStats
                    {
                        Precision = g.Key,
                        SizeBytes = g.Sum(m => m.SizeBytes),
                        ModelCount = g.Count(),
                        AverageSizeBytes = g.Average(m => m.SizeBytes)
                    }
                );

            return Task.FromResult(new ModelStorageStatistics
            {
                TotalSizeBytes = totalSize,
                TotalModels = totalModels,
                CachedModels = cachedModels,
                AvailableModels = totalModels,
                AvailableSpaceBytes = availableSpace,
                PlatformStats = platformStats.ToDictionary(kvp => ConvertToInfrastructurePlatformType(kvp.Key), kvp => kvp.Value),
                PrecisionStats = precisionStats
            });
        }

        #region Private Methods

        private long GetMockModelSize(Nexo.Core.Domain.Enums.PlatformType platform)
        {
            return platform switch
            {
                Nexo.Core.Domain.Enums.PlatformType.Web => 650L * 1024 * 1024, // 650MB
                Nexo.Core.Domain.Enums.PlatformType.Desktop => 4L * 1024 * 1024 * 1024, // 4GB
                Nexo.Core.Domain.Enums.PlatformType.Mobile => (long)(1.5 * 1024 * 1024 * 1024), // 1.5GB
                _ => 1L * 1024 * 1024 * 1024 // 1GB default
            };
        }

        private string CalculateChecksum(string filePath)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }

        private long GetAvailableDiskSpace(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:");
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 1024 * 1024 * 1024; // 1GB default
            }
        }

        #endregion

        private Nexo.Core.Domain.Entities.Infrastructure.PlatformType ConvertToInfrastructurePlatformType(Nexo.Core.Domain.Enums.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Enums.PlatformType.Web => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web,
                Nexo.Core.Domain.Enums.PlatformType.Desktop => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop,
                Nexo.Core.Domain.Enums.PlatformType.Mobile => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile,
                Nexo.Core.Domain.Enums.PlatformType.Windows => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows,
                Nexo.Core.Domain.Enums.PlatformType.Linux => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux,
                Nexo.Core.Domain.Enums.PlatformType.macOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS,
                Nexo.Core.Domain.Enums.PlatformType.iOS => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS,
                Nexo.Core.Domain.Enums.PlatformType.Android => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android,
                Nexo.Core.Domain.Enums.PlatformType.Cloud => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud,
                Nexo.Core.Domain.Enums.PlatformType.Container => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker,
                Nexo.Core.Domain.Enums.PlatformType.CrossPlatform => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other,
                _ => Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unknown
            };
        }
    }
}
