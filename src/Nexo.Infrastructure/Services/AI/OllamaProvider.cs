using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using ModelInfo = Nexo.Core.Domain.Entities.AI.ModelInfo;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Ollama provider implementation for offline LLama AI integration
    /// </summary>
    public class OllamaProvider : ILlamaProvider, Nexo.Core.Application.Interfaces.AI.IModelProvider, Nexo.Core.Application.Interfaces.AI.IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaProvider> _logger;
        private readonly string _baseUrl;
        private readonly HashSet<string> _loadedModels = new();

        public OllamaProvider(
            HttpClient httpClient,
            ILogger<OllamaProvider> logger,
            string baseUrl = "http://localhost:11434")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = baseUrl;

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-LLama-Provider/1.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Longer timeout for model operations
        }

        // ILlamaProvider implementation
        public string ProviderId => "ollama";
        public string DisplayName => "Ollama";
        public string Description => "Local Ollama models for offline AI operations";
        public int Priority => 95; // Higher priority than remote providers
        public bool IsOfflineCapable => true;
        public bool SupportsGpuAcceleration => true; // Ollama supports GPU acceleration
        public bool SupportsStreaming => true;
        public int MaxContextLength => 8192; // Typical for Ollama models
        public string ModelsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "models", "ollama");

        public IEnumerable<string> SupportedModelTypes => new[]
        {
            "TextGeneration",
            "CodeGeneration",
            "Chat"
        };

        public bool IsModelLoaded(string modelName)
        {
            return _loadedModels.Contains(modelName);
        }

        public async Task LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (IsModelLoaded(modelName))
            {
                _logger.LogDebug("Model {ModelName} is already loaded", modelName);
                return;
            }

            try
            {
                _logger.LogInformation("Loading model {ModelName} into Ollama", modelName);
                
                var request = new
                {
                    name = modelName,
                    stream = false
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _loadedModels.Add(modelName);
                    _logger.LogInformation("Successfully loaded model {ModelName}", modelName);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new InvalidOperationException($"Failed to load model {modelName}: {response.StatusCode} - {errorContent}");
                }
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
                _logger.LogInformation("Unloading model {ModelName} from Ollama", modelName);
                
                // Ollama doesn't have a specific unload API, but we can remove from our tracking
                _loadedModels.Remove(modelName);
                
                _logger.LogInformation("Successfully unloaded model {ModelName}", modelName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading model {ModelName}", modelName);
                throw;
            }
        }

        public async Task<long> GetModelMemoryUsageAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/ps", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var psResponse = JsonSerializer.Deserialize<OllamaPsResponse>(json);
                    
                    var model = psResponse?.Models?.FirstOrDefault(m => m.Name == modelName);
                    return model?.Size ?? 0;
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
                _logger.LogInformation("Downloading model {ModelName} via Ollama", modelName);
                
                var request = new
                {
                    name = modelName,
                    stream = false
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/pull", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Get model info after download
                var models = await GetAvailableModelsAsync(cancellationToken);
                var modelInfo = models.FirstOrDefault(m => m.Name == modelName);
                
                if (modelInfo == null)
                {
                    throw new InvalidOperationException($"Model {modelName} was downloaded but not found in available models");
                }

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
                _logger.LogInformation("Removing model {ModelName} via Ollama", modelName);
                
                var request = new
                {
                    name = modelName
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.DeleteAsync($"api/delete", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _loadedModels.Remove(modelName);
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
            // For Ollama, available models are the same as downloaded models
            return await GetAvailableModelsAsync(cancellationToken);
        }

        // IModelProvider implementation
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {

                var response = await _httpClient.GetAsync("api/tags", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                    
                    var models = modelsResponse?.Models?.Select(m => new ModelInfo
                    {
                        Name = m.Name,
                        SizeBytes = m.Size,
                        Capabilities = new Dictionary<string, object>
                        {
                            ["SupportsTextGeneration"] = true,
                            ["SupportsCodeGeneration"] = m.Name.Contains("code") || m.Name.Contains("codellama"),
                            ["SupportsAnalysis"] = true,
                            ["SupportsOptimization"] = false,
                            ["SupportsStreaming"] = true
                        }
                    }) ?? [];
                    
                    return models;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Ollama models");
            }

            return [];
        }

        public async Task<IModel> LoadModelForProviderAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await LoadModelAsync(modelName, cancellationToken);
            return new OllamaModel(modelName, _httpClient, _logger);
        }

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"ollama:response:{ComputeRequestHash(request)}";
            
            // Try to get from cache first (disabled for now)
            // var cachedResponse = await _cacheService.GetAsync<ModelResponse>(cacheKey, cancellationToken);
            // if (cachedResponse != null)
            // {
            //     _logger.LogDebug("Returning cached response for Ollama request");
            //     return cachedResponse;
            // }

            try
            {
                var startTime = DateTime.UtcNow;
                
                // Determine the model to use
                var model = GetModelFromRequest(request);
                
                // Create the Ollama request
                var ollamaRequest = CreateOllamaRequest(request, model);
                
                // Execute the request
                var response = await ExecuteOllamaRequestAsync(ollamaRequest, cancellationToken);
                
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                var modelResponse = new ModelResponse
                {
                    Response = response.Response,
                    InputTokens = EstimateTokenCount(request.Input),
                    OutputTokens = EstimateTokenCount(response.Response),
                    ProcessingTimeMs = executionTime,
                    ProviderId = ProviderId,
                    ModelName = model,
                    Metadata = new Dictionary<string, object>
                    {
                        ["done"] = response.Done,
                        ["context"] = response.Context,
                        ["cached"] = false
                    }
                };

                // Cache the response for 1 hour (disabled for now)
                // await _cacheService.SetAsync(cacheKey, modelResponse, TimeSpan.FromHours(1), cancellationToken);
                
                return modelResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Ollama request");
                throw;
            }
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var response = await _httpClient.GetAsync("api/tags", cancellationToken);
                var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                return new ModelHealthStatus
                {
                    IsHealthy = response.IsSuccessStatusCode,
                    Status = response.IsSuccessStatusCode ? "Healthy" : $"HTTP {response.StatusCode}",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = responseTime,
                    ErrorRate = response.IsSuccessStatusCode ? 0.0 : 1.0,
                    ProviderId = ProviderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Ollama health status");
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
        private static string GetModelFromRequest(ModelRequest request)
        {
            if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
            {
                return model;
            }

            return "llama2"; // Default model
        }

        private static bool IsModelSupported(string model)
        {
            var supportedModels = new[] { "llama2", "llama2:7b", "llama2:13b", "llama2:70b", "codellama", "codellama:7b", "codellama:13b", "mistral", "mistral:7b" };
            return supportedModels.Contains(model.ToLower());
        }

        private object CreateOllamaRequest(ModelRequest request, string model)
        {
            return new
            {
                model = model,
                prompt = CreatePrompt(request),
                stream = false,
                options = new
                {
                    temperature = request.Context?.TryGetValue("temperature", out var tempObj) == true ? tempObj : request.Temperature,
                    top_p = request.Context?.TryGetValue("top_p", out var topPObj) == true ? topPObj : 0.9,
                    num_predict = request.Context?.TryGetValue("max_tokens", out var maxTokensObj) == true ? maxTokensObj : request.MaxTokens
                }
            };
        }

        private static string CreatePrompt(ModelRequest request)
        {
            var prompt = new StringBuilder();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                prompt.AppendLine($"System: {request.SystemPrompt}");
                prompt.AppendLine();
            }

            prompt.Append(request.Input);
            return prompt.ToString();
        }

        private async Task<OllamaResponse> ExecuteOllamaRequestAsync(object request, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<OllamaResponse>(responseContent) ?? new OllamaResponse();
        }

        private static int EstimateTokenCount(string text)
        {
            // Simple estimation: ~4 characters per token
            return Math.Max(1, text.Length / 4);
        }

        private static ModelType GetModelType(string modelId)
        {
            if (modelId.Contains("code") || modelId.Contains("codellama"))
            {
                return ModelType.CodeGeneration;
            }
            if (modelId.Contains("chat"))
            {
                return ModelType.Chat;
            }
            return ModelType.TextGeneration;
        }

        private static string ComputeRequestHash(ModelRequest request)
        {
            var input = $"{request.Input}|{request.SystemPrompt}|{request.Temperature}|{request.MaxTokens}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash)[..16]; // First 16 characters
        }

        // Response models
        private class OllamaResponse
        {
            public string Model { get; set; } = string.Empty;
            public string Response { get; set; } = string.Empty;
            public bool Done { get; set; }
            public List<long> Context { get; set; } = new();
        }

        private class OllamaModelsResponse
        {
            public List<OllamaModelData> Models { get; set; } = new();
        }

        private class OllamaModelData
        {
            public string Name { get; set; } = string.Empty;
            public string Digest { get; set; } = string.Empty;
            public long Size { get; set; }
        }

        private class OllamaPsResponse
        {
            public List<OllamaModelProcess> Models { get; set; } = new();
        }

        private class OllamaModelProcess
        {
            public string Name { get; set; } = string.Empty;
            public long Size { get; set; }
        }

        // IModelProvider implementation
        public AIProviderType ProviderType => AIProviderType.Ollama;
        public string Name => "Ollama Provider";
        public string Version => "1.0.0";

        public bool IsAvailable()
        {
            return true; // Ollama is always available if running
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
            return model.ModelType == "Llama" || model.ModelType == "TextGeneration";
        }

        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            var models = await GetAvailableModelsAsync();
            return models.FirstOrDefault(m => m.Name == modelId) ?? new ModelInfo
            {
                Name = modelId,
                Description = modelId,
                EngineType = AIEngineType.Llama,
                Status = ModelStatus.Unavailable
            };
        }

        // IAIProvider implementation
        public AIProviderCapabilities Capabilities => new AIProviderCapabilities
        {
            ProviderType = AIProviderType.Ollama,
            SupportedPlatforms = new List<PlatformType> { PlatformType.Windows, PlatformType.Linux, PlatformType.macOS },
            SupportedOperations = new List<AIOperationType> { AIOperationType.CodeGeneration, AIOperationType.CodeReview, AIOperationType.CodeOptimization },
            SupportsOfflineMode = true,
            SupportsStreaming = true,
            SupportsBatchProcessing = false,
            MaxConcurrentOperations = 1
        };

        public AIProviderStatus Status => AIProviderStatus.Available;

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
            return true; // Ollama manages its own resources
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("OllamaProvider initialized");
            await Task.CompletedTask;
        }

        public async Task<IAIEngine> CreateEngineAsync(AIOperationContext context)
        {
            var modelName = context.ModelName ?? "llama2";
            await LoadModelAsync(modelName);
            // Return a mock engine for now since OllamaModel doesn't implement IAIEngine
            return new MockAIEngine();
        }

        public async Task<ModelInfo> DownloadModelForAIProviderAsync(string modelId, string variantId)
        {
            await DownloadModelAsync(modelId, CancellationToken.None);
            return new ModelInfo
            {
                Name = modelId,
                Description = modelId,
                EngineType = AIEngineType.Llama,
                Status = ModelStatus.Available
            };
        }

        public async Task<Nexo.Core.Domain.Results.PerformanceEstimate> EstimatePerformanceAsync(AIOperationContext context)
        {
            await Task.CompletedTask;
            return new Nexo.Core.Domain.Results.PerformanceEstimate
            {
                EstimatedDuration = TimeSpan.FromSeconds(3),
                EstimatedMemoryUsage = 512 * 1024 * 1024, // 512MB
                EstimatedCpuUsage = 0.6,
                Confidence = 0.8
            };
        }

        public bool SupportsEngineType(AIEngineType engineType)
        {
            return engineType == AIEngineType.Llama;
        }

        public AIEngineType EngineType => AIEngineType.Llama;
        public AIProviderType Provider => AIProviderType.Ollama;

        // IAIProvider.DownloadModelAsync implementation
        async Task<ModelInfo> IAIProvider.DownloadModelAsync(string modelId, string variantId)
        {
            return await DownloadModelForAIProviderAsync(modelId, variantId);
        }
    }

}
