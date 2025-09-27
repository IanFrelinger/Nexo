using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Models
{
    /// <summary>
    /// Storage and file management functionality for RealModelManagementService
    /// </summary>
    public partial class RealModelManagementService
    {
        public Task<string> GetModelPathAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            try
            {
                _logger.LogInformation("Getting model path for {ModelId} on platform {Platform}", modelId, platform);

                var modelPath = Path.Combine(_modelsDirectory, modelId, $"{modelId}_{platform}.model");
                
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model file not found: {modelPath}");
                }

                return Task.FromResult(modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model path for {ModelId}", modelId);
                throw;
            }
        }

        public Task<bool> IsModelAvailableAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            try
            {
                _logger.LogInformation("Checking if model {ModelId} is available for platform {Platform}", modelId, platform);

                var modelPath = Path.Combine(_modelsDirectory, modelId, $"{modelId}_{platform}.model");
                return Task.FromResult(File.Exists(modelPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking model availability for {ModelId}", modelId);
                return Task.FromResult(false);
            }
        }

        public async Task<List<ModelInfo>> ListModelsAsync(Nexo.Core.Domain.Enums.PlatformType platform)
        {
            try
            {
                _logger.LogInformation("Listing models for platform {Platform}", platform);

                var models = new List<ModelInfo>();
                
                if (Directory.Exists(_modelsDirectory))
                {
                    var directories = Directory.GetDirectories(_modelsDirectory);
                    
                    foreach (var directory in directories)
                    {
                        var modelId = Path.GetFileName(directory);
                        var modelInfo = await GetModelInfoAsync(modelId);
                        
                        if (modelInfo != null && modelInfo.Platform.Contains(platform))
                        {
                            models.Add(modelInfo);
                        }
                    }
                }

                _logger.LogInformation("Found {Count} models for platform {Platform}", models.Count, platform);
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models for platform {Platform}", platform);
                throw;
            }
        }

        public Task RemoveModelAsync(string modelId, Nexo.Core.Domain.Enums.PlatformType platform)
        {
            try
            {
                _logger.LogInformation("Removing model {ModelId} for platform {Platform}", modelId, platform);

                var modelPath = Path.Combine(_modelsDirectory, modelId, $"{modelId}_{platform}.model");
                
                if (File.Exists(modelPath))
                {
                    File.Delete(modelPath);
                }

                // Remove directory if empty
                var modelDirectory = Path.Combine(_modelsDirectory, modelId);
                if (Directory.Exists(modelDirectory) && !Directory.EnumerateFileSystemEntries(modelDirectory).Any())
                {
                    Directory.Delete(modelDirectory);
                }

                _logger.LogInformation("Successfully removed model {ModelId}", modelId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing model {ModelId}", modelId);
                throw;
            }
        }

        public Task CleanupModelsAsync(TimeSpan maxAge)
        {
            try
            {
                _logger.LogInformation("Cleaning up models older than {MaxAge}", maxAge);

                if (Directory.Exists(_modelsDirectory))
                {
                    var directories = Directory.GetDirectories(_modelsDirectory);
                    var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
                    
                    foreach (var directory in directories)
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        if (directoryInfo.LastWriteTime < cutoffTime)
                        {
                            Directory.Delete(directory, true);
                            _logger.LogInformation("Cleaned up old model directory: {Directory}", directory);
                        }
                    }
                }

                _logger.LogInformation("Model cleanup completed");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during model cleanup");
                throw;
            }
        }

        private string GetModelPath(string modelId, string version)
        {
            var versionFolder = version ?? "latest";
            var fileName = $"{modelId}.gguf";
            return Path.Combine(_modelsDirectory, versionFolder, fileName);
        }
    }
}
