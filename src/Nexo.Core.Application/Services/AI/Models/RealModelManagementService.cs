using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Models
{
    /// <summary>
    /// Real model management service for downloading and managing AI models
    /// </summary>
    public class RealModelManagementService : IModelManagementService
    {
        private readonly ILogger<RealModelManagementService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _modelsDirectory;
        private readonly Dictionary<string, ModelInfo> _cachedModels;

        public RealModelManagementService(ILogger<RealModelManagementService> logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _modelsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nexo", "Models");
            _cachedModels = new Dictionary<string, ModelInfo>();
            
            // Ensure models directory exists
            Directory.CreateDirectory(_modelsDirectory);
        }

        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            try
            {
                _logger.LogInformation("Getting model info for {ModelId}", modelId);

                // Check cache first
                if (_cachedModels.ContainsKey(modelId))
                {
                    return _cachedModels[modelId];
                }

                // Get model info from registry
                var modelInfo = await GetModelInfoFromRegistryAsync(modelId);
                
                // Cache the result
                _cachedModels[modelId] = modelInfo;
                
                return modelInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get model info for {ModelId}", modelId);
                throw;
            }
        }

        public async Task<bool> DownloadModelAsync(string modelId, string version = null)
        {
            try
            {
                _logger.LogInformation("Downloading model {ModelId} version {Version}", modelId, version ?? "latest");

                // Get model info
                var modelInfo = await GetModelInfoAsync(modelId);
                
                // Check if model is already downloaded
                var modelPath = GetModelPath(modelId, version);
                if (File.Exists(modelPath))
                {
                    _logger.LogInformation("Model {ModelId} already exists at {ModelPath}", modelId, modelPath);
                    return true;
                }

                // Download model
                await DownloadModelFileAsync(modelInfo, modelPath);

                // Verify download
                if (!await VerifyModelDownloadAsync(modelPath, modelInfo))
                {
                    _logger.LogError("Model download verification failed for {ModelId}", modelId);
                    return false;
                }

                _logger.LogInformation("Model {ModelId} downloaded successfully to {ModelPath}", modelId, modelPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<bool> IsModelAvailableAsync(string modelId, string version = null)
        {
            try
            {
                var modelPath = GetModelPath(modelId, version);
                return File.Exists(modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check model availability for {ModelId}", modelId);
                return false;
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

        public async Task<bool> DeleteModelAsync(string modelId, string version = null)
        {
            try
            {
                _logger.LogInformation("Deleting model {ModelId} version {Version}", modelId, version ?? "latest");

                var modelPath = GetModelPath(modelId, version);
                
                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Model {ModelId} not found at {ModelPath}", modelId, modelPath);
                    return false;
                }

                // Delete model file
                File.Delete(modelPath);

                // Remove from cache
                _cachedModels.Remove(modelId);

                _logger.LogInformation("Model {ModelId} deleted successfully", modelId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<ModelStorageStatistics> GetStorageStatisticsAsync()
        {
            try
            {
                _logger.LogInformation("Getting model storage statistics");

                var statistics = new ModelStorageStatistics
                {
                    TotalModels = 0,
                    TotalSize = 0,
                    AvailableSpace = 0,
                    PlatformType = GetCurrentPlatformType(),
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
                        statistics.TotalSize += fileInfo.Length;
                    }
                }

                // Get available space
                var driveInfo = new DriveInfo(Path.GetPathRoot(_modelsDirectory));
                statistics.AvailableSpace = driveInfo.AvailableFreeSpace;

                _logger.LogInformation("Storage statistics: {TotalModels} models, {TotalSize} bytes, {AvailableSpace} bytes available", 
                    statistics.TotalModels, statistics.TotalSize, statistics.AvailableSpace);

                return statistics;
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
                Size = 4 * 1024 * 1024 * 1024, // 4GB
                Format = "GGUF",
                Quantization = ModelQuantization.Q4_0,
                Status = ModelStatus.Available,
                DownloadUrl = $"https://huggingface.co/microsoft/{modelId}/resolve/main/model.gguf",
                Checksum = "mock-checksum",
                SupportedPlatforms = new[] { PlatformType.Windows, PlatformType.macOS, PlatformType.Linux },
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task DownloadModelFileAsync(ModelInfo modelInfo, string modelPath)
        {
            _logger.LogInformation("Downloading model file from {DownloadUrl}", modelInfo.DownloadUrl);

            try
            {
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(modelPath);
                Directory.CreateDirectory(directory);

                // Download file
                using (var response = await _httpClient.GetAsync(modelInfo.DownloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    
                    using (var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                _logger.LogInformation("Model file downloaded successfully to {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download model file from {DownloadUrl}", modelInfo.DownloadUrl);
                throw;
            }
        }

        private async Task<bool> VerifyModelDownloadAsync(string modelPath, ModelInfo modelInfo)
        {
            try
            {
                var fileInfo = new FileInfo(modelPath);
                
                // Check file size
                if (fileInfo.Length != modelInfo.Size)
                {
                    _logger.LogError("Model file size mismatch. Expected: {ExpectedSize}, Actual: {ActualSize}", 
                        modelInfo.Size, fileInfo.Length);
                    return false;
                }

                // In a real implementation, verify checksum
                // For now, just check if file exists and has correct size
                await Task.Delay(50);
                
                _logger.LogInformation("Model download verification successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify model download");
                return false;
            }
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
                        SupportedPlatforms = new[] { GetCurrentPlatformType() },
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
                    Size = 4 * 1024 * 1024 * 1024, // 4GB
                    Format = "GGUF",
                    Quantization = ModelQuantization.Q4_0,
                    Status = ModelStatus.Available,
                    DownloadUrl = "https://huggingface.co/microsoft/Llama-2-7b-chat-gguf/resolve/main/llama-2-7b-chat.q4_0.gguf",
                    SupportedPlatforms = new[] { PlatformType.Windows, PlatformType.macOS, PlatformType.Linux },
                    LastUpdated = DateTime.UtcNow
                },
                new ModelInfo
                {
                    ModelId = "codellama-7b-instruct",
                    Name = "CodeLlama 7B Instruct",
                    Version = "1.0.0",
                    Size = 4 * 1024 * 1024 * 1024, // 4GB
                    Format = "GGUF",
                    Quantization = ModelQuantization.Q4_0,
                    Status = ModelStatus.Available,
                    DownloadUrl = "https://huggingface.co/microsoft/CodeLlama-7b-Instruct-gguf/resolve/main/codellama-7b-instruct.q4_0.gguf",
                    SupportedPlatforms = new[] { PlatformType.Windows, PlatformType.macOS, PlatformType.Linux },
                    LastUpdated = DateTime.UtcNow
                }
            };
        }

        private string GetModelPath(string modelId, string version)
        {
            var versionFolder = version ?? "latest";
            var fileName = $"{modelId}.gguf";
            return Path.Combine(_modelsDirectory, versionFolder, fileName);
        }

        private PlatformType GetCurrentPlatformType()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return PlatformType.Windows;
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                return PlatformType.macOS;
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                return PlatformType.Linux;
            else
                return PlatformType.Unknown;
        }
    }
}
