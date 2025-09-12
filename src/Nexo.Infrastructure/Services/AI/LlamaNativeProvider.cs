using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.Results;
using ModelInfo = Nexo.Core.Domain.Entities.AI.ModelInfo;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Native LLama provider implementation using LlamaSharp for offline AI operations
    /// </summary>
    public class LlamaNativeProvider : ILlamaProvider, Nexo.Core.Application.Interfaces.AI.IModelProvider, Nexo.Core.Application.Interfaces.AI.IAIProvider
    {
        private readonly ILogger<LlamaNativeProvider> _logger;
        private readonly string _modelsDirectory;
        private readonly Dictionary<string, object> _loadedModels = new();
        private readonly object _modelsLock = new();
        private bool _isInitialized = false;

        public LlamaNativeProvider(
            ILogger<LlamaNativeProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "models", "native");
            
            // Ensure models directory exists
            Directory.CreateDirectory(_modelsDirectory);
        }

        // ILlamaProvider implementation
        public string ProviderId => "llama-native";
        public string DisplayName => "LLama Native";
        public string Name => "LLama Native";
        public string Description => "Native LLama implementation using LlamaSharp for offline AI operations";
        public string Version => "1.0.0";
        public AIProviderType ProviderType => AIProviderType.LlamaNative;
        public int Priority => 90; // Lower priority than Ollama but higher than remote providers
        public bool IsOfflineCapable => true;
        public bool SupportsGpuAcceleration => DetectGpuSupport();
        public bool SupportsStreaming => true;
        public int MaxContextLength => 4096; // Conservative default
        public string ModelsPath => _modelsDirectory;

        public IEnumerable<string> SupportedModelTypes => new[]
        {
            "TextGeneration",
            "CodeGeneration",
            "Chat"
        };

        public bool IsModelLoaded(string modelName)
        {
            lock (_modelsLock)
            {
                return _loadedModels.ContainsKey(modelName);
            }
        }

        public async Task LoadModelInternalAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (IsModelLoaded(modelName))
            {
                _logger.LogDebug("Model {ModelName} is already loaded", modelName);
                return;
            }

            try
            {
                _logger.LogInformation("Loading model {ModelName} into native LLama", modelName);
                
                var modelPath = GetModelPath(modelName);
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model file not found: {modelPath}");
                }

                // In a real implementation, you would use LlamaSharp here
                // For now, we'll simulate the loading process
                await Task.Delay(1000, cancellationToken); // Simulate loading time
                
                lock (_modelsLock)
                {
                    _loadedModels[modelName] = new { LoadedAt = DateTime.UtcNow, Path = modelPath };
                }

                _logger.LogInformation("Successfully loaded model {ModelName}", modelName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model {ModelName}", modelName);
                throw;
            }
        }

        public async Task UnloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (!IsModelLoaded(modelName))
            {
                _logger.LogDebug("Model {ModelName} is not loaded", modelName);
                return;
            }

            try
            {
                _logger.LogInformation("Unloading model {ModelName} from native LLama", modelName);
                
                lock (_modelsLock)
                {
                    _loadedModels.Remove(modelName);
                }
                
                // Force garbage collection to free memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                _logger.LogInformation("Successfully unloaded model {ModelName}", modelName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading model {ModelName}", modelName);
                throw;
            }
        }

        public async Task<long> GetModelMemoryUsageAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (!IsModelLoaded(modelName))
            {
                return 0;
            }

            try
            {
                var modelPath = GetModelPath(modelName);
                if (File.Exists(modelPath))
                {
                    var fileInfo = new FileInfo(modelPath);
                    return fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory usage for model {ModelName}", modelName);
            }

            return 0;
        }

        public async Task<ModelInfo> DownloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Downloading model {ModelName} for native LLama", modelName);
                
                var modelPath = GetModelPath(modelName);
                var modelUrl = GetModelDownloadUrl(modelName);
                
                if (string.IsNullOrEmpty(modelUrl))
                {
                    throw new InvalidOperationException($"No download URL available for model {modelName}");
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

                // Download the model file
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(modelUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                var modelInfo = new ModelInfo
                {
                    Name = modelName,
                    Description = modelName,
                    EngineType = AIEngineType.LlamaNative,
                    Status = ModelStatus.Available,
                    SizeBytes = new FileInfo(modelPath).Length,
                    Parameters = new Dictionary<string, object>
                    {
                        ["SupportsTextGeneration"] = true,
                        ["SupportsCodeGeneration"] = modelName.Contains("code"),
                        ["SupportsAnalysis"] = true,
                        ["SupportsOptimization"] = false,
                        ["SupportsStreaming"] = SupportsStreaming,
                        ["SupportsChat"] = true
                    },
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("Successfully downloaded model {ModelName}", modelName);
                return modelInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading model {ModelName}", modelName);
                throw;
            }
        }

        public async Task<bool> RemoveModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Removing model {ModelName} from native LLama", modelName);
                
                // Unload if currently loaded
                if (IsModelLoaded(modelName))
                {
                    await UnloadModelAsync(modelName, cancellationToken);
                }

                var modelPath = GetModelPath(modelName);
                if (File.Exists(modelPath))
                {
                    File.Delete(modelPath);
                    _logger.LogInformation("Successfully removed model {ModelName}", modelName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing model {ModelName}", modelName);
                return false;
            }
        }

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsForDownloadAsync(CancellationToken cancellationToken = default)
        {
            // Return a list of available models for download
            var availableModels = new[]
            {
                "llama-2-7b-chat.gguf",
                "llama-2-13b-chat.gguf",
                "codellama-7b-instruct.gguf",
                "codellama-13b-instruct.gguf",
                "mistral-7b-instruct.gguf"
            };

            return availableModels.Select(name => new ModelInfo
            {
                Name = name,
                SizeBytes = 0,
                Description = $"Available model: {name}",
                EngineType = AIEngineType.LlamaNative,
                Status = ModelStatus.Available
            });
        }

        // IModelProvider implementation
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {

                var models = new List<ModelInfo>();
                
                if (Directory.Exists(_modelsDirectory))
                {
                    var modelFiles = Directory.GetFiles(_modelsDirectory, "*.gguf");
                    
                    foreach (var modelFile in modelFiles)
                    {
                        var fileName = Path.GetFileName(modelFile);
                        var fileInfo = new FileInfo(modelFile);
                        
                        models.Add(new ModelInfo
                        {
                            Name = fileName,
                            SizeBytes = fileInfo.Length,
                            LastUpdated = fileInfo.LastWriteTimeUtc
                        });
                    }
                }

                // Return models
                
                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching native LLama models");
                return [];
            }
        }

        // ILlamaProvider version
        public async Task LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await LoadModelInternalAsync(modelName, cancellationToken);
        }

        // IModelProvider version
        public async Task<IModel> LoadModelForProviderAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await LoadModelInternalAsync(modelName, cancellationToken);
            return new LlamaNativeModel(modelName, _logger, this);
        }

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            // Process request

            try
            {
                var startTime = DateTime.UtcNow;
                
                // Determine the model to use
                var model = GetModelFromRequest(request);
                
                // Ensure model is loaded
                if (!IsModelLoaded(model))
                {
                    await LoadModelInternalAsync(model, cancellationToken);
                }

                // In a real implementation, you would use LlamaSharp to generate the response
                // For now, we'll simulate the response
                var response = await SimulateNativeResponseAsync(request, model, cancellationToken);
                
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                var modelResponse = new ModelResponse
                {
                    Response = response,
                    InputTokens = EstimateTokenCount(request.Input),
                    OutputTokens = EstimateTokenCount(response),
                    ProcessingTimeMs = executionTime,
                    Metadata = new Dictionary<string, object>
                    {
                        ["cached"] = false,
                        ["native"] = true
                    }
                };

                // Return response
                
                return modelResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing native LLama request");
                throw;
            }
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Check if we can access the models directory
                var canAccess = Directory.Exists(_modelsDirectory) && Directory.GetFiles(_modelsDirectory).Length > 0;
                
                var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                return new ModelHealthStatus
                {
                    IsHealthy = canAccess,
                    Status = canAccess ? "Healthy" : "No models available",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = responseTime,
                    ErrorRate = canAccess ? 0.0 : 1.0,
                    ProviderId = ProviderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking native LLama health status");
                return new ModelHealthStatus
                {
                    IsHealthy = false,
                    Status = $"Error: {ex.Message}",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = 0,
                    ErrorRate = 1.0,
                    ProviderId = ProviderId
                };
            }
        }

        // Helper methods
        private string GetModelPath(string modelName)
        {
            return Path.Combine(_modelsDirectory, modelName);
        }

        private static string GetModelDownloadUrl(string modelName)
        {
            // In a real implementation, you would have a registry of model URLs
            // For now, return null to indicate no download available
            return null;
        }

        private static string GetModelFromRequest(ModelRequest request)
        {
            if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
            {
                return model;
            }

            return "llama-2-7b-chat.gguf"; // Default model
        }

        private static ModelType GetModelType(string modelId)
        {
            if (modelId.Contains("code"))
            {
                return ModelType.CodeGeneration;
            }
            if (modelId.Contains("chat"))
            {
                return ModelType.Chat;
            }
            return ModelType.TextGeneration;
        }

        private static int EstimateTokenCount(string text)
        {
            // Simple estimation: ~4 characters per token
            return Math.Max(1, text.Length / 4);
        }

        private static string ComputeRequestHash(ModelRequest request)
        {
            var input = $"{request.Input}|{request.SystemPrompt}|{request.Temperature}|{request.MaxTokens}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash)[..16]; // First 16 characters
        }

        private static bool DetectGpuSupport()
        {
            try
            {
                // Check for CUDA availability
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return File.Exists(@"C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.0\bin\nvcc.exe");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return File.Exists("/usr/local/cuda/bin/nvcc");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Check for Metal support on macOS
                    return true; // Assume Metal is available on modern Macs
                }
            }
            catch
            {
                // Ignore errors and assume no GPU support
            }

            return false;
        }

        private async Task<string> SimulateNativeResponseAsync(ModelRequest request, string model, CancellationToken cancellationToken)
        {
            // Simulate processing time based on model size
            var delay = model.Contains("13b") ? 3000 : 1500;
            await Task.Delay(delay, cancellationToken);

            // Generate a simple response based on the input
            var responses = new[]
            {
                "I understand your request. Let me help you with that.",
                "That's an interesting question. Here's what I think...",
                "Based on your input, I can provide the following guidance:",
                "I can assist you with that. Here's my analysis:",
                "Great question! Here's my response:"
            };

            var random = new Random();
            var baseResponse = responses[random.Next(responses.Length)];
            
            // Add some context-specific response
            if (request.Input.Contains("code") || request.Input.Contains("programming"))
            {
                return $"{baseResponse}\n\nThis appears to be a programming-related question. I can help you with code generation, debugging, or architectural decisions.";
            }
            else if (request.Input.Contains("test") || request.Input.Contains("testing"))
            {
                return $"{baseResponse}\n\nI can help you with testing strategies, test case generation, or test automation approaches.";
            }
            else
            {
                return $"{baseResponse}\n\nI'm here to help with your development needs. Feel free to ask more specific questions.";
            }
        }

        // IModelProvider implementation
        public bool IsAvailable()
        {
            return _isInitialized;
        }

        public async Task<List<ModelInfo>> GetAvailableModelsAsync()
        {
            var models = await GetAvailableModelsForDownloadAsync();
            return models.ToList();
        }

        public async Task<bool> DownloadModelAsync(string modelId, string? variant = null)
        {
            await LoadModelAsync(modelId);
            return true;
        }

        public bool IsModelCompatible(ModelInfo model)
        {
            return true; // All models are compatible
        }

        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            var models = await GetAvailableModelsForDownloadAsync();
            return models.FirstOrDefault(m => m.Name == modelId) ?? new ModelInfo
            {
                Name = modelId,
                Description = modelId,
                EngineType = AIEngineType.LlamaNative,
                Status = ModelStatus.Unavailable
            };
        }

        // IAIProvider implementation
        public AIProviderCapabilities Capabilities => new AIProviderCapabilities
        {
            ProviderType = AIProviderType.Llama,
            SupportedPlatforms = new List<PlatformType> { PlatformType.Windows, PlatformType.Linux, PlatformType.macOS },
            SupportedOperations = new List<AIOperationType> { AIOperationType.CodeGeneration, AIOperationType.CodeReview, AIOperationType.CodeOptimization },
            SupportsOfflineMode = true,
            SupportsStreaming = true,
            SupportsBatchProcessing = false,
            MaxConcurrentOperations = 1
        };

        public AIProviderStatus Status => _isInitialized ? AIProviderStatus.Available : AIProviderStatus.Initializing;

        public bool SupportsPlatform(Nexo.Core.Domain.Enums.PlatformType platform)
        {
            return platform == Nexo.Core.Domain.Enums.PlatformType.Windows || platform == Nexo.Core.Domain.Enums.PlatformType.Linux || platform == Nexo.Core.Domain.Enums.PlatformType.macOS;
        }

        public bool MeetsRequirements(AIRequirements requirements)
        {
            return requirements.RequiresOfflineMode && IsOfflineCapable;
        }

        public bool HasRequiredResources(AIResources resources)
        {
            return true; // Native provider manages its own resources
        }

        public async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                _logger.LogInformation("LlamaNativeProvider initialized");
            }
            await Task.CompletedTask;
        }

        public async Task<IAIEngine> CreateEngineAsync(AIOperationContext context)
        {
            var modelName = context.ModelName ?? "llama-2-7b-chat";
            await LoadModelAsync(modelName);
            // Return a mock engine for now since LlamaNativeModel doesn't implement IAIEngine
            return new MockAIEngine();
        }

        public async Task<ModelInfo> DownloadModelForAIProviderAsync(string modelId, string variantId)
        {
            await DownloadModelAsync(modelId, CancellationToken.None);
            return new ModelInfo
            {
                Name = modelId,
                Description = modelId,
                EngineType = AIEngineType.LlamaNative,
                Status = ModelStatus.Available
            };
        }

        public async Task<Nexo.Core.Domain.Results.PerformanceEstimate> EstimatePerformanceAsync(AIOperationContext context)
        {
            return new Nexo.Core.Domain.Results.PerformanceEstimate
            {
                EstimatedDuration = TimeSpan.FromSeconds(5),
                EstimatedMemoryUsage = 1024 * 1024 * 1024, // 1GB
                EstimatedCpuUsage = 0.8,
                Confidence = 0.7
            };
        }

        public bool SupportsEngineType(AIEngineType engineType)
        {
            return engineType == AIEngineType.LlamaNative;
        }

        public AIEngineType EngineType => AIEngineType.LlamaNative;
        public AIProviderType Provider => AIProviderType.Llama;

        // IAIProvider.DownloadModelAsync implementation
        async Task<ModelInfo> IAIProvider.DownloadModelAsync(string modelId, string variantId)
        {
            return await DownloadModelForAIProviderAsync(modelId, variantId);
        }
    }

    /// <summary>
    /// Mock AI Engine for testing
    /// </summary>
    public class MockAIEngine : IAIEngine
    {
        public AIEngineInfo EngineInfo => new AIEngineInfo
        {
            Name = "Mock Engine",
            Version = "1.0.0",
            EngineType = AIEngineType.Mock,
            IsAvailable = true
        };

        public AIOperationStatus Status => AIOperationStatus.Completed;
        public bool IsInitialized => true;

        public Task InitializeAsync(ModelInfo model, AIOperationContext context) => Task.CompletedTask;
        public Task<CodeGenerationResult> GenerateCodeAsync(CodeGenerationRequest request) => Task.FromResult(new CodeGenerationResult());
        public Task<CodeReviewResult> ReviewCodeAsync(string code, AIOperationContext context) => Task.FromResult(new CodeReviewResult());
        public Task<CodeGenerationResult> OptimizeCodeAsync(string code, AIOperationContext context) => Task.FromResult(new CodeGenerationResult());
        public Task<string> GenerateDocumentationAsync(string code, AIOperationContext context) => Task.FromResult("Mock documentation");
        public Task<CodeGenerationResult> GenerateTestsAsync(string code, AIOperationContext context) => Task.FromResult(new CodeGenerationResult());
        public Task<CodeGenerationResult> RefactorCodeAsync(string code, AIOperationContext context) => Task.FromResult(new CodeGenerationResult());
        public Task<AIResponse> AnalyzeCodeAsync(string code, AIOperationContext context) => Task.FromResult(new AIResponse());
        public Task<CodeGenerationResult> TranslateCodeAsync(string code, string targetLanguage, AIOperationContext context) => Task.FromResult(new CodeGenerationResult());
        public Task<AIResponse> GenerateResponseAsync(string prompt, AIOperationContext context) => Task.FromResult(new AIResponse());
        public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, AIOperationContext context)
        {
            yield break;
        }
        public Task CancelAsync() => Task.CompletedTask;
        public Task DisposeAsync() => Task.CompletedTask;
        public long GetMemoryUsage() => 0;
        public double GetCpuUsage() => 0.0;
        public bool IsHealthy() => true;
    }

    /// <summary>
    /// Native LLama model implementation
    /// </summary>
    public class LlamaNativeModel : IModel
    {
        private readonly string _modelName;
        private readonly ILogger _logger;
        private readonly LlamaNativeProvider _provider;
        private ModelInfo? _info;

        public LlamaNativeModel(string modelName, ILogger logger, LlamaNativeProvider provider)
        {
            _modelName = modelName;
            _logger = logger;
            _provider = provider;
        }

        public ModelInfo Info => _info ??= new ModelInfo
        {
            Name = _modelName,
            Description = _modelName,
            EngineType = AIEngineType.LlamaNative,
            Status = ModelStatus.Available,
            SizeBytes = 0, // Will be populated when needed
            Parameters = new Dictionary<string, object>
            {
                ["SupportsTextGeneration"] = true,
                ["SupportsCodeGeneration"] = _modelName.Contains("code"),
                ["SupportsAnalysis"] = true,
                ["SupportsOptimization"] = false,
                ["SupportsStreaming"] = _provider.SupportsStreaming,
                ["SupportsChat"] = true
            },
            LastUpdated = DateTime.UtcNow
        };

        public bool IsLoaded => _provider.IsModelLoaded(_modelName);
        public string ModelId => _modelName;
        public string Name => _modelName;
        public Nexo.Feature.AI.Enums.ModelType ModelType => Nexo.Feature.AI.Enums.ModelType.TextGeneration;

        public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            return await _provider.ExecuteAsync(request, cancellationToken);
        }

        public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming not implemented yet");
        }

        public ModelCapabilities GetCapabilities()
        {
            return new ModelCapabilities
            {
                SupportsTextGeneration = true,
                SupportsCodeGeneration = _modelName.Contains("code"),
                SupportsAnalysis = true,
                SupportsOptimization = false,
                SupportsStreaming = _provider.SupportsStreaming
            };
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return _provider.LoadModelAsync(_modelName, cancellationToken);
        }

        public Task UnloadAsync(CancellationToken cancellationToken = default)
        {
            return _provider.UnloadModelAsync(_modelName, cancellationToken);
        }

        // IModelProvider implementation
        public bool IsAvailable()
        {
            return true; // Native provider is always available
        }


        // ILlamaProvider version
        public async Task<ModelInfo> DownloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                // For native provider, we assume models are manually placed
                // This is a stub implementation
                _logger.LogInformation("Download model {ModelName} requested, but native provider requires manual model placement", modelName);
                return new ModelInfo
                {
                    Name = modelName,
                    Description = modelName,
                    EngineType = AIEngineType.LlamaNative,
                    Status = ModelStatus.Unavailable
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download model {ModelName}", modelName);
                return new ModelInfo
                {
                    Name = modelName,
                    Description = modelName,
                    EngineType = AIEngineType.LlamaNative,
                    Status = ModelStatus.Unavailable
                };
            }
        }

        // IModelProvider version
        public async Task<bool> DownloadModelAsync(string modelId, string? variant = null)
        {
            try
            {
                // For native provider, we assume models are manually placed
                _logger.LogInformation("Download model {ModelId} requested, but native provider requires manual model placement", modelId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsForDownloadAsync(CancellationToken cancellationToken = default)
        {
            // For native provider, return empty list as models are manually placed
            return new List<ModelInfo>();
        }

        public bool IsModelCompatible(ModelInfo model)
        {
            return true; // All models are compatible
        }

        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            var models = await GetAvailableModelsForDownloadAsync();
            return models.FirstOrDefault(m => m.Name == modelId) ?? new ModelInfo
            {
                Name = modelId,
                Description = modelId,
                EngineType = AIEngineType.LlamaNative,
                Status = ModelStatus.Unavailable
            };
        }
    }
}
