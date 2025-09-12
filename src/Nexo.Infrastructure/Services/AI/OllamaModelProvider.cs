using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using IModelProvider = Nexo.Feature.AI.Interfaces.IModelProvider;
using ModelInfo = Nexo.Feature.AI.Models.ModelInfo;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// Ollama model provider implementation.
/// </summary>
public class OllamaModelProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaModelProvider> _logger;

    public OllamaModelProvider(
        HttpClient httpClient,
        ILogger<OllamaModelProvider> logger,
        string baseUrl = "http://localhost:11434")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
    }

    // IModelProvider interface implementation
    public string ProviderId => "ollama";
    public string DisplayName => "Ollama";
    public string Description => "Local Ollama models for text generation and chat";
    public bool IsAvailable => true; // Assume available if we can reach the service

    public IEnumerable<ModelType> SupportedModelTypes =>
    [
        ModelType.TextGeneration,
        ModelType.CodeGeneration
    ];

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                return modelsResponse?.Models?.Select(m => new ModelInfo
                {
                    Name = m.Name,
                    DisplayName = m.Name,
                    ModelType = GetModelType(m.Name),
                    IsAvailable = true,
                    SizeBytes = m.Size,
                    MaxContextLength = 4096,
                    Capabilities = new ModelCapabilities
                    {
                        SupportsTextGeneration = true,
                        SupportsCodeGeneration = true,
                        SupportsAnalysis = true,
                        SupportsOptimization = false,
                        SupportsStreaming = true
                    }
                }) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Ollama models");
        }

        return [];
    }

    public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
        if (modelInfo == null)
        {
            throw new InvalidOperationException($"Model {modelName} not found or not available");
        }

        return new OllamaModel(modelName, _httpClient, _logger);
    }

    public async Task<ModelInfo?> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var models = await GetAvailableModelsAsync(cancellationToken);
        return models.FirstOrDefault(m => m.Name == modelName);
    }

    public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var errors = new List<string>();

        try
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                errors.Add($"Model {modelName} not found");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error validating model: {ex.Message}");
        }

        return new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
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
                ErrorRate = response.IsSuccessStatusCode ? 0.0 : 1.0
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
                ErrorRate = 1.0
            };
        }
    }

    // Legacy methods for backward compatibility
    public string Name => DisplayName;
    public string ProviderType => "Ollama";
    public bool IsEnabled => true;
    public bool IsPrimary => false;

    public ModelCapabilities Capabilities => new ModelCapabilities
    {
        SupportsTextGeneration = true,
        SupportsCodeGeneration = true,
        SupportsAnalysis = true,
        SupportsOptimization = false,
        SupportsStreaming = true
    };

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Executing Ollama request with model {Model}", request.Context?.TryGetValue("model", out var modelObj) == true ? modelObj as string : "llama2");

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
            
            return new ModelResponse
            {
                Response = response.Response,
                InputTokens = 0,
                OutputTokens = 0,
                ProcessingTimeMs = executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["done"] = response.Done,
                    ["context"] = response.Context
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Ollama request");
            throw;
        }
    }

    public Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrEmpty(request.Input))
        {
            errors.Add("Input is required");
        }

        // Validate model
        var model = GetModelFromRequest(request);
        if (!IsModelSupported(model))
        {
            errors.Add($"Model {model} is not supported");
        }

        // Validate token limits
        var estimatedTokens = EstimateTokenCount(request.Input);
        if (estimatedTokens > 4096)
        {
            errors.Add("Input exceeds maximum token limit");
        }

        return Task.FromResult(new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }

    private static string GetModelFromRequest(ModelRequest request)
    {
        // Try to get model from context first
        if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
        {
            return model;
        }

        // Default to llama2
        return "llama2";
    }

    private static bool IsModelSupported(string model)
    {
        var supportedModels = new[] { "llama2", "llama2:7b", "llama2:13b", "llama2:70b", "codellama", "mistral" };
        return supportedModels.Contains(model.ToLower());
    }

    private object CreateOllamaRequest(ModelRequest request, string model)
    {
        var ollamaRequest = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = CreatePrompt(request),
            ["stream"] = false,
            ["options"] = new Dictionary<string, object>
            {
                ["temperature"] = request.Context?.TryGetValue("temperature", out var tempObj) == true ? tempObj : request.Temperature,
                ["top_p"] = request.Context?.TryGetValue("top_p", out var topPObj) == true ? topPObj : 0.9,
                ["num_predict"] = request.Context?.TryGetValue("max_tokens", out var maxTokensObj) == true ? maxTokensObj : request.MaxTokens
            }
        };

        return ollamaRequest;
    }

    private static string CreatePrompt(ModelRequest request)
    {
        var prompt = new StringBuilder();

        // Add system message if provided
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            prompt.AppendLine($"System: {request.SystemPrompt}");
            prompt.AppendLine();
        }

        // Add user message
        prompt.Append(request.Input);

        return prompt.ToString();
    }

    private async Task<OllamaResponse> ExecuteOllamaRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OllamaResponse>(responseContent) ?? new OllamaResponse();
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return text.Length / 4;
    }

    private static ModelType GetModelType(string modelId)
    {
        if (modelId.Contains("code") || modelId.Contains("codellama"))
        {
            return ModelType.CodeGeneration;
        }
        return ModelType.TextGeneration;
    }

    private class OllamaResponse
    {
        public string Model { get; set; }
        public string Response { get; set; }
        public bool Done { get; set; }
        public List<long> Context { get; set; }
        public OllamaResponse()
        {
            Model = string.Empty;
            Response = string.Empty;
            Done = false;
            Context = [];
        }
    }

    private class OllamaModelsResponse
    {
        public List<OllamaModelData> Models { get; set; }
        public OllamaModelsResponse()
        {
            Models = [];
        }
    }

    private class OllamaModelData
    {
        public string Name { get; set; }
        public string Digest { get; set; }
        public long Size { get; set; }
        public OllamaModelData()
        {
            Name = string.Empty;
            Digest = string.Empty;
            Size = 0;
        }
    }
}

/// <summary>
/// Ollama model implementation.
/// </summary>
public class OllamaModel : IModel
{
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private ModelInfo? _info;

    public OllamaModel(string modelName, HttpClient httpClient, ILogger logger)
    {
        _modelName = modelName;
        _httpClient = httpClient;
        _logger = logger;
    }

    public ModelInfo Info
    {
        get
        {
            return _info ??= new ModelInfo
            {
                Name = _modelName,
                DisplayName = _modelName,
                ModelType = Nexo.Feature.AI.Enums.ModelType.TextGeneration,
                IsAvailable = true,
                SizeBytes = 1024 * 1024 * 1024,
                MaxContextLength = 4096,
                Capabilities = new ModelCapabilities
                {
                    SupportsTextGeneration = true,
                    SupportsCodeGeneration = true,
                    SupportsAnalysis = true,
                    SupportsOptimization = false,
                    SupportsStreaming = false
                }
            };
        }
    }

    public bool IsLoaded => true;

    /// <summary>
    /// Unique identifier for this model instance
    /// </summary>
    public string ModelId => _modelName;

    /// <summary>
    /// Human-readable name of the model
    /// </summary>
    public string Name => _modelName;

    /// <summary>
    /// Type of model
    /// </summary>
    public Nexo.Feature.AI.Enums.ModelType ModelType => Nexo.Feature.AI.Enums.ModelType.TextGeneration;

    public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        // Create a new provider instance with a null logger to avoid type issues
        var provider = new OllamaModelProvider(_httpClient, NullLogger<OllamaModelProvider>.Instance);
        return await provider.ExecuteAsync(request, cancellationToken);
    }

    public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException("Streaming not implemented yet");
    }

    public ModelCapabilities GetCapabilities()
    {
        return new ModelCapabilities
        {
            SupportsTextGeneration = true,
            SupportsCodeGeneration = true,
            SupportsAnalysis = true,
            SupportsOptimization = false,
            SupportsStreaming = true
        };
    }

    public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // Ollama models are loaded on demand
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // No cleanup needed for Ollama models
        return Task.CompletedTask;
    }
}
}
