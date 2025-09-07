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
using static Nexo.Feature.AI.Enums.ModelType;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// OpenAI model provider implementation.
/// </summary>
public class OpenAiModelProvider : IModelProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiModelProvider> _logger;
    private readonly string _apiKey;
    private readonly Dictionary<string, object> _defaultParameters;

    public OpenAiModelProvider(
        HttpClient httpClient,
        ILogger<OpenAiModelProvider> logger,
        string apiKey,
        string baseUrl = "https://api.openai.com/v1")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        // Configure default parameters
        _defaultParameters = new Dictionary<string, object>
        {
            ["temperature"] = 0.7,
            ["max_tokens"] = 2000,
            ["top_p"] = 1.0,
            ["frequency_penalty"] = 0.0,
            ["presence_penalty"] = 0.0
        };

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
    }

    // IModelProvider interface implementation
    public string ProviderId => "openai";
    public string DisplayName => "OpenAI";
    public string Description => "OpenAI GPT models for text generation and chat";
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public IEnumerable<ModelType> SupportedModelTypes =>
    [
        ModelType.TextGeneration,
        ModelType.CodeGeneration,
        ModelType.TextEmbedding
    ];

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(json);
                return modelsResponse?.Data?.Select(m => new ModelInfo
                {
                    Name = m.Id,
                    DisplayName = m.Id,
                    ModelType = GetModelType(m.Id),
                    IsAvailable = true,
                    SizeBytes = 1024 * 1024 * 1024,
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
            _logger.LogError(ex, "Error fetching OpenAI models");
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

        return new OpenAiModel(modelName, _httpClient, _logger);
    }

    public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
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
            var response = await _httpClient.GetAsync("models", cancellationToken);
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
            _logger.LogError(ex, "Error checking OpenAI health status");
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
    public string ProviderType => "OpenAI";
    public bool IsEnabled => IsAvailable;
    public bool IsPrimary => true;

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
        _logger.LogInformation("Executing OpenAI request with model {Model}", request.Context?.TryGetValue("model", out var modelObj) == true ? modelObj as string : "gpt-4");

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Determine the model to use
            var model = GetModelFromRequest(request);
            
            // Create the OpenAI request
            var openAiRequest = CreateOpenAiRequest(request, model);
            
            // Execute the request
            var response = await ExecuteOpenAiRequestAsync(openAiRequest, cancellationToken);
            
            var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return new ModelResponse
            {
                Response = response.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                InputTokens = response.Usage?.PromptTokens ?? 0,
                OutputTokens = response.Usage?.CompletionTokens ?? 0,
                ProcessingTimeMs = executionTime,
                Metadata = new Dictionary<string, object>
                {
                    ["finish_reason"] = response.Choices?.FirstOrDefault()?.FinishReason ?? "unknown",
                    ["usage"] = response.Usage
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OpenAI request");
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
        if (estimatedTokens > 128000)
        {
            errors.Add("Input exceeds maximum token limit");
        }

        return Task.FromResult(new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }

    private string GetModelFromRequest(ModelRequest request)
    {
        // Try to get model from context first
        if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
        {
            return model;
        }

        // Default to GPT-4
        return "gpt-4";
    }

    private static bool IsModelSupported(string model)
    {
        var supportedModels = new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "gpt-3.5-turbo-16k" };
        return supportedModels.Contains(model.ToLower());
    }

    private object CreateOpenAiRequest(ModelRequest request, string model)
    {
        var openAiRequest = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = CreateMessages(request),
            ["temperature"] = request.Context?.TryGetValue("temperature", out var tempObj) == true ? tempObj : request.Temperature,
            ["max_tokens"] = request.Context?.TryGetValue("max_tokens", out var maxTokensObj) == true ? maxTokensObj : request.MaxTokens,
            ["top_p"] = request.Context?.TryGetValue("top_p", out var topPObj) == true ? topPObj : 0.9,
            ["frequency_penalty"] = request.Context?.TryGetValue("frequency_penalty", out var freqPenaltyObj) == true ? freqPenaltyObj : 0.0,
            ["presence_penalty"] = request.Context?.TryGetValue("presence_penalty", out var presPenaltyObj) == true ? presPenaltyObj : 0.0
        };

        return openAiRequest;
    }

    private List<object> CreateMessages(ModelRequest request)
    {
        var messages = new List<object>();

        // Add system message if provided
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        // Add user message
        messages.Add(new { role = "user", content = request.Input });

        return messages;
    }

    private async Task<OpenAiResponse> ExecuteOpenAiRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OpenAiResponse>(responseContent) ?? new OpenAiResponse();
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return text.Length / 4;
    }

    private static ModelType GetModelType(string modelId)
    {
        if (modelId.Contains("gpt"))
        {
            return modelId.Contains('4') ? TextGeneration : TextGeneration;
        }
        return TextGeneration;
    }

    private class OpenAiResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public List<OpenAiChoice> Choices { get; set; }
        public OpenAiUsage Usage { get; set; }
        public OpenAiResponse()
        {
            Id = string.Empty;
            Object = string.Empty;
            Created = 0;
            Model = string.Empty;
            Choices = new List<OpenAiChoice>();
            Usage = new OpenAiUsage();
        }
    }

    private class OpenAiChoice
    {
        public int Index { get; set; }
        public OpenAiMessage Message { get; set; }
        public string FinishReason { get; set; }
        public OpenAiChoice()
        {
            Index = 0;
            Message = new OpenAiMessage();
            FinishReason = string.Empty;
        }
    }

    private class OpenAiMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public OpenAiMessage()
        {
            Role = string.Empty;
            Content = string.Empty;
        }
    }

    private class OpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public OpenAiUsage()
        {
            PromptTokens = 0;
            CompletionTokens = 0;
            TotalTokens = 0;
        }
    }

    private class OpenAiModelsResponse
    {
        public List<OpenAiModelData> Data { get; set; }
        public OpenAiModelsResponse()
        {
            Data = [];
        }
    }

    private class OpenAiModelData
    {
        public string Id { get; set; }
        public string OwnedBy { get; set; }
        public List<object> Permission { get; set; }
        public OpenAiModelData()
        {
            Id = string.Empty;
            OwnedBy = string.Empty;
            Permission = new List<object>();
        }
    }
}

/// <summary>
/// OpenAI model implementation.
/// </summary>
public class OpenAiModel : IModel
{
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private ModelInfo? _info;

    public OpenAiModel(string modelName, HttpClient httpClient, ILogger logger)
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
                    SupportsStreaming = true
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
        var provider = new OpenAiModelProvider(_httpClient, NullLogger<OpenAiModelProvider>.Instance, "dummy-key");
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
        // OpenAI models are loaded on demand
        return Task.CompletedTask;
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // No cleanup needed for OpenAI models
        return Task.CompletedTask;
    }
}
}
