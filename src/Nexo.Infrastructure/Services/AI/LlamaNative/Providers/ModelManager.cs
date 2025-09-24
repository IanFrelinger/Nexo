using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.LlamaNative.Providers
{
    public class ModelManager
    {
        private readonly ILogger<ModelManager> _logger;
        private readonly Dictionary<string, object> _loadedModels = new();
        private readonly object _modelsLock = new();
        private bool _isInitialized = false;

        public string ModelsDirectory { get; }

        public ModelManager(ILogger<ModelManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ModelsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "models", "native");
            
            // Ensure models directory exists
            Directory.CreateDirectory(ModelsDirectory);
        }

        public bool IsModelLoaded(string modelName)
        {
            lock (_modelsLock)
            {
                return _loadedModels.ContainsKey(modelName);
            }
        }

        public async Task<bool> LoadModelAsync(string modelName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading model: {ModelName}", modelName);

                lock (_modelsLock)
                {
                    if (_loadedModels.ContainsKey(modelName))
                    {
                        _logger.LogInformation("Model {ModelName} is already loaded", modelName);
                        return true;
                    }
                }

                // Simulate model loading
                await Task.Delay(1000, cancellationToken);

                lock (_modelsLock)
                {
                    _loadedModels[modelName] = new { LoadedAt = DateTime.UtcNow };
                }

                _logger.LogInformation("Model {ModelName} loaded successfully", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model {ModelName}", modelName);
                return false;
            }
        }

        public async Task<bool> UnloadModelAsync(string modelName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Unloading model: {ModelName}", modelName);

                lock (_modelsLock)
                {
                    if (!_loadedModels.ContainsKey(modelName))
                    {
                        _logger.LogInformation("Model {ModelName} is not loaded", modelName);
                        return true;
                    }

                    _loadedModels.Remove(modelName);
                }

                // Simulate model unloading
                await Task.Delay(500, cancellationToken);

                _logger.LogInformation("Model {ModelName} unloaded successfully", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading model {ModelName}", modelName);
                return false;
            }
        }

        public async Task<ModelListResponse> ListModelsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Listing available models");

                var models = new List<ModelInfo>();

                // Check for available model files
                var modelFiles = Directory.GetFiles(ModelsDirectory, "*.gguf", SearchOption.AllDirectories);
                
                foreach (var file in modelFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new FileInfo(file);
                    
                    models.Add(new ModelInfo
                    {
                        Id = fileName,
                        Object = "model",
                        Created = fileInfo.CreationTimeUtc,
                        OwnedBy = "llama",
                        Permission = new List<string> { "read" },
                        Root = fileName,
                        Parent = null
                    });
                }

                // Add some default models if none found
                if (models.Count == 0)
                {
                    models.AddRange(GetDefaultModels());
                }

                return new ModelListResponse
                {
                    Success = true,
                    Models = models
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                return new ModelListResponse
                {
                    Success = false,
                    Error = $"Model listing failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_isInitialized)
                    return true;

                _logger.LogInformation("Initializing model manager");
                
                // Ensure models directory exists
                Directory.CreateDirectory(ModelsDirectory);
                
                // Simulate initialization
                await Task.Delay(100, cancellationToken);
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing model manager");
                return false;
            }
        }

        private List<ModelInfo> GetDefaultModels()
        {
            return new List<ModelInfo>
            {
                new ModelInfo
                {
                    Id = "llama-2-7b-chat",
                    Object = "model",
                    Created = DateTime.UtcNow.AddDays(-30),
                    OwnedBy = "llama",
                    Permission = new List<string> { "read" },
                    Root = "llama-2-7b-chat",
                    Parent = null
                },
                new ModelInfo
                {
                    Id = "llama-2-13b-chat",
                    Object = "model",
                    Created = DateTime.UtcNow.AddDays(-25),
                    OwnedBy = "llama",
                    Permission = new List<string> { "read" },
                    Root = "llama-2-13b-chat",
                    Parent = null
                },
                new ModelInfo
                {
                    Id = "codellama-7b",
                    Object = "model",
                    Created = DateTime.UtcNow.AddDays(-20),
                    OwnedBy = "llama",
                    Permission = new List<string> { "read" },
                    Root = "codellama-7b",
                    Parent = null
                }
            };
        }
    }
}
