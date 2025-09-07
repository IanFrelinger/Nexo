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
        TextGeneration,
        CodeGeneration,
        TextEmbedding
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
                return modelsResponse?.Data?.Select(m => new ModelInfo(1024 * 1024 * 1024, 1000000000, 4096)
                {
                    Id = m.Id,
                    Name = m.Id,
                    Description = $"OpenAI {m.Id} model",
                    Version = "1.0",
                    Type = GetModelType(m.Id),
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["owned_by"] = m.OwnedBy,
                        ["permission"] = m.Permission
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
        return models.FirstOrDefault(m => m.Id == modelName || m.Name == modelName);
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
            var response = await _httpClient.GetAsync("models", cancellationToken);
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
            _logger.LogError(ex, "Error checking OpenAI health status");
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
    public string ProviderType => "OpenAI";
    public bool IsEnabled => IsAvailable;
    public bool IsPrimary => true;

    public ModelCapabilities Capabilities => new ModelCapabilities(true, true, true, false, false)
    {
        SupportsStreaming = true,
        SupportsFunctionCalling = true,
        SupportsTextEmbedding = true,
        MaxInputLength = 128000,
        MaxOutputLength = 128000,
        SupportedLanguages = ["en", "es", "fr", "de", "it", "pt", "nl", "pl", "ru", "ja", "ko", "zh"]
    };

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
    {
        _logger.LogInformation("Executing OpenAI request with model {Model}", request.Metadata.TryGetValue("model", out var modelObj) ? modelObj as string : "gpt-4");

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
            
            return new ModelResponse(response.Usage?.PromptTokens ?? 0, response.Usage?.CompletionTokens ?? 0)
            {
                Content = response.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                Model = model,
                TotalTokens = response.Usage?.TotalTokens ?? 0,
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
        if (estimatedTokens <= 128000) return Task.FromResult(result);
        result.IsValid = false;
        result.Errors.Add("Input exceeds maximum token limit");

        return Task.FromResult(result);
    }

    private string GetModelFromRequest(ModelRequest request)
    {
        // Try to get model from metadata first
        if (request.Metadata.TryGetValue("model", out var modelObj) && modelObj is string model)
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
            ["temperature"] = request.Metadata.TryGetValue("temperature", out var tempObj) ? tempObj : _defaultParameters["temperature"],
            ["max_tokens"] = request.Metadata.TryGetValue("max_tokens", out var maxTokensObj) ? maxTokensObj : _defaultParameters["max_tokens"],
            ["top_p"] = request.Metadata.TryGetValue("top_p", out var topPObj) ? topPObj : _defaultParameters["top_p"],
            ["frequency_penalty"] = request.Metadata.TryGetValue("frequency_penalty", out var freqPenaltyObj) ? freqPenaltyObj : _defaultParameters["frequency_penalty"],
            ["presence_penalty"] = request.Metadata.TryGetValue("presence_penalty", out var presPenaltyObj) ? presPenaltyObj : _defaultParameters["presence_penalty"]
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
    private ModelInfo _info;

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
            return _info ??= new ModelInfo(1024 * 1024 * 1024, 1000000000, 4096)
            {
                Id = _modelName,
                Name = _modelName,
                Description = "OpenAI " + _modelName + " model",
                Version = "1.0",
                Type = TextGeneration,
                Provider = "openai",
                IsAvailable = true,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public bool IsLoaded => true;

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
        return new ModelCapabilities(true, true, true, false, false)
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsTextEmbedding = true,
            MaxInputLength = 128000,
            MaxOutputLength = 128000,
            SupportedLanguages = ["en", "es", "fr", "de", "it", "pt", "nl", "pl", "ru", "ja", "ko", "zh"]
        };
    }

    public Task UnloadAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        // No cleanup needed for OpenAI models
        return Task.CompletedTask;
    }
}
}
