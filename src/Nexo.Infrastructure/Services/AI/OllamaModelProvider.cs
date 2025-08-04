using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// Ollama model provider implementation.
/// </summary>
public class OllamaModelProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaModelProvider> _logger;
    private readonly string _baseUrl;

    public OllamaModelProvider(
        HttpClient httpClient,
        ILogger<OllamaModelProvider> logger,
        string baseUrl = "http://localhost:11434")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = baseUrl;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
    }

    // IModelProvider interface implementation
    public string ProviderId => "ollama";
    public string DisplayName => "Ollama";
    public string Description => "Local Ollama models for text generation and chat";
    public bool IsAvailable => true; // Assume available if we can reach the service

    public IEnumerable<ModelType> SupportedModelTypes => new[]
    {
        ModelType.TextGeneration,
        ModelType.CodeGeneration
    };

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                OllamaModelsResponse modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                return modelsResponse?.Models?.Select(m => new ModelInfo
                {
                    Id = m.Name,
                    Name = m.Name,
                    Description = $"Ollama {m.Name} model",
                    Version = m.Digest?.Substring(0, Math.Min(12, m.Digest.Length)) ?? "latest",
                    Type = GetModelType(m.Name),
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["digest"] = m.Digest,
                        ["size"] = m.Size
                    }
                }) ?? Enumerable.Empty<ModelInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Ollama models");
        }

        return Enumerable.Empty<ModelInfo>();
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

    public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var models = await GetAvailableModelsAsync(cancellationToken);
        foreach (var m in models)
        {
            if (m.Id == modelName || m.Name == modelName)
                return m;
        }
        return null;
    }

    public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var result = new ModelValidationResult { IsValid = true };

        try
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Model {modelName} not found");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Error validating model: {ex.Message}");
        }

        return result;
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
                ProviderName = DisplayName,
                IsHealthy = response.IsSuccessStatusCode,
                ResponseTimeMs = responseTime,
                ErrorRate = response.IsSuccessStatusCode ? 0.0 : 1.0,
                LastError = response.IsSuccessStatusCode ? "" : $"HTTP {response.StatusCode}",
                LastCheckTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama health status");
            return new ModelHealthStatus
            {
                ProviderName = DisplayName,
                IsHealthy = false,
                ResponseTimeMs = 0,
                ErrorRate = 1.0,
                LastError = ex.Message,
                LastCheckTime = DateTime.UtcNow
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
        SupportsStreaming = true,
        SupportsFunctionCalling = false,
        SupportsTextEmbedding = false,
        MaxInputLength = 4096,
        MaxOutputLength = 4096,
        SupportedLanguages = new List<string> { "en" }
    };

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Executing Ollama request with model {Model}", request.Metadata.TryGetValue("model", out var modelObj) ? modelObj as string : "llama2");

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
                Content = response.Response,
                Model = model,
                TotalTokens = 0, // Ollama doesn't provide token usage
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

    public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
    {
        var result = new ModelValidationResult { IsValid = true };

        // Validate required fields
        if (string.IsNullOrEmpty(request.Input))
        {
            result.IsValid = false;
            result.Errors.Add("Input is required");
        }

        // Validate model
        var model = GetModelFromRequest(request);
        if (!IsModelSupported(model))
        {
            result.IsValid = false;
            result.Errors.Add($"Model {model} is not supported");
        }

        // Validate token limits
        var estimatedTokens = EstimateTokenCount(request.Input);
        if (estimatedTokens > 4096)
        {
            result.IsValid = false;
            result.Errors.Add("Input exceeds maximum token limit");
        }

        return result;
    }

    private string GetModelFromRequest(ModelRequest request)
    {
        // Try to get model from metadata first
        if (request.Metadata.TryGetValue("model", out var modelObj) && modelObj is string model)
        {
            return model;
        }

        // Default to llama2
        return "llama2";
    }

    private bool IsModelSupported(string model)
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
                ["temperature"] = request.Metadata.TryGetValue("temperature", out var tempObj) ? tempObj : 0.7,
                ["top_p"] = request.Metadata.TryGetValue("top_p", out var topPObj) ? topPObj : 0.9,
                ["num_predict"] = request.Metadata.TryGetValue("max_tokens", out var maxTokensObj) ? maxTokensObj : 2000
            }
        };

        return ollamaRequest;
    }

    private string CreatePrompt(ModelRequest request)
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

    private int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return text.Length / 4;
    }

    private ModelType GetModelType(string modelId)
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
            Context = new List<long>();
        }
    }

    private class OllamaModelsResponse
    {
        public List<OllamaModelData> Models { get; set; }
        public OllamaModelsResponse()
        {
            Models = new List<OllamaModelData>();
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
    private ModelInfo _info;

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
            if (_info == null)
            {
                _info = new ModelInfo
                {
                    Id = _modelName,
                    Name = _modelName,
                    Description = "Ollama " + _modelName + " model",
                    Version = "latest",
                    Type = ModelType.TextGeneration,
                    Provider = "ollama",
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow
                };
            }
            return _info;
        }
    }

    public bool IsLoaded => true;

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
            SupportsStreaming = true,
            SupportsFunctionCalling = false,
            SupportsTextEmbedding = false,
            MaxInputLength = 4096,
            MaxOutputLength = 4096,
            SupportedLanguages = new List<string> { "en" }
        };
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // No cleanup needed for Ollama models
        return Task.CompletedTask;
    }
}
}
