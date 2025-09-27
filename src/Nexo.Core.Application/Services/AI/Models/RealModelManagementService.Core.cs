using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Models
{
    /// <summary>
    /// Core functionality for RealModelManagementService
    /// </summary>
    public partial class RealModelManagementService
    {
        public async Task<ModelInfo?> GetModelInfoAsync(string modelId, string? version = null)
        {
            try
            {
                _logger.LogInformation("Getting model info for {ModelId} version {Version}", modelId, version ?? "latest");

                // Check cache first
                if (_cachedModels.TryGetValue(modelId, out var cachedModel))
                {
                    _logger.LogInformation("Model {ModelId} found in cache", modelId);
                    return cachedModel;
                }

                // Get model info from registry
                var modelInfo = await GetModelInfoFromRegistryAsync(modelId);
                
                // Cache the model info
                _cachedModels[modelId] = modelInfo;
                
                _logger.LogInformation("Model info retrieved for {ModelId}", modelId);
                return modelInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get model info for {ModelId}", modelId);
                throw;
            }
        }

        public Task<bool> IsModelAvailableAsync(string modelId, string? version = null)
        {
            try
            {
                var modelPath = GetModelPath(modelId, version ?? "latest");
                return Task.FromResult(File.Exists(modelPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check model availability for {ModelId}", modelId);
                return Task.FromResult(false);
            }
        }

        public async Task<List<ModelInfo>> GetAvailableModelsAsync()
        {
            try
            {
                _logger.LogInformation("Getting available models");

                var models = new List<ModelInfo>();
                
                // Get models from local directory
                var localModels = await GetLocalModelsAsync();
                models.AddRange(localModels);

                // Get models from registry
                var registryModels = await GetRegistryModelsAsync();
                models.AddRange(registryModels);

                _logger.LogInformation("Found {Count} available models", models.Count);
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available models");
                throw;
            }
        }

        public Task<bool> DeleteModelAsync(string modelId, string? version = null)
        {
            try
            {
                _logger.LogInformation("Deleting model {ModelId} version {Version}", modelId, version ?? "latest");

                var modelPath = GetModelPath(modelId, version ?? "latest");
                
                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Model {ModelId} not found at {ModelPath}", modelId, modelPath);
                    return Task.FromResult(false);
                }

                // Delete model file
                File.Delete(modelPath);

                // Remove from cache
                _cachedModels.Remove(modelId);

                _logger.LogInformation("Model {ModelId} deleted successfully", modelId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete model {ModelId}", modelId);
                return Task.FromResult(false);
            }
        }

        public Task<ModelStorageStatistics> GetStorageStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting model storage statistics");

                var statistics = new ModelStorageStatistics
                {
                    TotalModels = 0,
                    TotalSizeBytes = 0,
                    AvailableSpaceBytes = 0,
                    PlatformType = ConvertToInfrastructurePlatformType(GetCurrentPlatformType()),
                    LastUpdated = DateTime.UtcNow
                };

                // Calculate statistics
                if (Directory.Exists(_modelsDirectory))
                {
                    var modelFiles = Directory.GetFiles(_modelsDirectory, "*.gguf", SearchOption.AllDirectories);
                    statistics.TotalModels = modelFiles.Length;

                    foreach (var file in modelFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        statistics.TotalSizeBytes += fileInfo.Length;
                    }
                }

                // Get available space
                var driveInfo = new DriveInfo(Path.GetPathRoot(_modelsDirectory) ?? "C:\\");
                statistics.AvailableSpaceBytes = driveInfo.AvailableFreeSpace;

                _logger.LogInformation("Storage statistics: {TotalModels} models, {TotalSize} bytes, {AvailableSpace} bytes available", 
                    statistics.TotalModels, statistics.TotalSize, statistics.TotalSize);

                return Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage statistics");
                throw;
            }
        }

        private async Task<ModelInfo> GetModelInfoFromRegistryAsync(string modelId)
        {
            // In a real implementation, this would query a model registry
            // For now, return mock data
            await Task.Delay(100);

            return new ModelInfo
            {
                ModelId = modelId,
                Name = $"Model {modelId}",
                Version = "1.0.0",
                Size = 4L * 1024 * 1024 * 1024, // 4GB
                Format = "GGUF",
                Quantization = ModelQuantization.Q4_0,
                Status = ModelStatus.Available,
                DownloadUrl = $"https://huggingface.co/microsoft/{modelId}/resolve/main/model.gguf",
                Checksum = "mock-checksum",
                SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { Nexo.Core.Domain.Enums.PlatformType.Windows, Nexo.Core.Domain.Enums.PlatformType.macOS, Nexo.Core.Domain.Enums.PlatformType.Linux },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<List<ModelInfo>> GetLocalModelsAsync()
        {
            var models = new List<ModelInfo>();

            if (Directory.Exists(_modelsDirectory))
            {
                var modelFiles = Directory.GetFiles(_modelsDirectory, "*.gguf", SearchOption.AllDirectories);
                
                foreach (var file in modelFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var modelId = Path.GetFileNameWithoutExtension(file);
                    
                    models.Add(new ModelInfo
                    {
                        ModelId = modelId,
                        Name = $"Local {modelId}",
                        Version = "1.0.0",
                        Size = fileInfo.Length,
                        Format = "GGUF",
                        Quantization = ModelQuantization.Q4_0,
                        Status = ModelStatus.Available,
                        LocalPath = file,
                        SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { GetCurrentPlatformType() },
                        LastUpdated = fileInfo.LastWriteTime
                    });
                }
            }

            await Task.Delay(100);
            return models;
        }

        private async Task<List<ModelInfo>> GetRegistryModelsAsync()
        {
            // In a real implementation, this would query a model registry
            // For now, return mock data
            await Task.Delay(100);

            return new List<ModelInfo>
            {
                new ModelInfo
                {
                    ModelId = "llama-2-7b-chat",
                    Name = "Llama 2 7B Chat",
                    Version = "1.0.0",
                    Size = 4L * 1024 * 1024 * 1024, // 4GB
                    Format = "GGUF",
                    Quantization = ModelQuantization.Q4_0,
                    Status = ModelStatus.Available,
                    DownloadUrl = "https://huggingface.co/microsoft/Llama-2-7b-chat-gguf/resolve/main/llama-2-7b-chat.q4_0.gguf",
                    SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { Nexo.Core.Domain.Enums.PlatformType.Windows, Nexo.Core.Domain.Enums.PlatformType.macOS, Nexo.Core.Domain.Enums.PlatformType.Linux },
                    LastUpdated = DateTime.UtcNow
                },
                new ModelInfo
                {
                    ModelId = "codellama-7b-instruct",
                    Name = "CodeLlama 7B Instruct",
                    Version = "1.0.0",
                    Size = 4L * 1024 * 1024 * 1024, // 4GB
                    Format = "GGUF",
                    Quantization = ModelQuantization.Q4_0,
                    Status = ModelStatus.Available,
                    DownloadUrl = "https://huggingface.co/microsoft/CodeLlama-7b-Instruct-gguf/resolve/main/codellama-7b-instruct.q4_0.gguf",
                    SupportedPlatforms = new List<Nexo.Core.Domain.Enums.PlatformType> { Nexo.Core.Domain.Enums.PlatformType.Windows, Nexo.Core.Domain.Enums.PlatformType.macOS, Nexo.Core.Domain.Enums.PlatformType.Linux },
                    LastUpdated = DateTime.UtcNow
                }
            };
        }
    }
}
