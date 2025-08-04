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
    /// Azure OpenAI model provider implementation.
    /// </summary>
    public class AzureOpenAIModelProvider : IModelProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureOpenAIModelProvider> _logger;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _apiVersion;
        private readonly Dictionary<string, object> _defaultParameters;

        public AzureOpenAIModelProvider(
            HttpClient httpClient,
            ILogger<AzureOpenAIModelProvider> logger,
            string apiKey,
            string endpoint,
            string apiVersion = "2023-05-15"
        )
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _endpoint = endpoint?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(endpoint));
            _apiVersion = apiVersion;

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
            _httpClient.BaseAddress = new Uri(_endpoint);
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-AI-Provider/1.0");
        }

        public string ProviderId => "azure-openai";
        public string DisplayName => "Azure OpenAI";
        public string Description => "Azure-hosted OpenAI GPT models for text generation and chat";
        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_endpoint);
        
        // IModelProvider interface properties
        public string Name => DisplayName;
        public string ProviderType => "AzureOpenAI";
        public bool IsEnabled => IsAvailable;
        public bool IsPrimary => false;

        public IEnumerable<ModelType> SupportedModelTypes => new[]
        {
            ModelType.TextGeneration,
            ModelType.CodeGeneration,
            ModelType.TextEmbedding
        };

        public ModelCapabilities Capabilities => new ModelCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = true,
            SupportsTextEmbedding = true,
            MaxInputLength = 128000,
            MaxOutputLength = 128000,
            SupportedLanguages = new List<string> { "en", "es", "fr", "de", "it", "pt", "nl", "pl", "ru", "ja", "ko", "zh" }
        };

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            // Azure OpenAI does not have a public models endpoint; assume deployments are known/configured.
            // For demo, return a static list or fetch from a config/service if available.
            // In production, you may want to load from configuration or a management API.
            return new List<ModelInfo>
            {
                new ModelInfo
                {
                    Id = "gpt-35-turbo",
                    Name = "gpt-35-turbo",
                    Description = "Azure OpenAI GPT-3.5 Turbo",
                    Version = _apiVersion,
                    Type = ModelType.TextGeneration,
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow
                },
                new ModelInfo
                {
                    Id = "gpt-4",
                    Name = "gpt-4",
                    Description = "Azure OpenAI GPT-4",
                    Version = _apiVersion,
                    Type = ModelType.TextGeneration,
                    Provider = ProviderId,
                    IsAvailable = true,
                    LastUpdated = DateTime.UtcNow
                }
            };
        }

        public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                throw new InvalidOperationException($"Model {modelName} not found or not available");
            }
            return new AzureOpenAIModel(modelName, _httpClient, _logger, _apiVersion);
        }

        public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default)
        {
            var models = await GetAvailableModelsAsync(cancellationToken);
            return models.FirstOrDefault(m => m.Id == modelName || m.Name == modelName);
        }

        public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default)
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

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing Azure OpenAI request");
            try
            {
                var startTime = DateTime.UtcNow;
                var model = request.Metadata.TryGetValue("model", out var modelObj) ? modelObj as string : "gpt-35-turbo";
                var url = $"openai/deployments/{model}/chat/completions?api-version={_apiVersion}";
                var azureRequest = CreateAzureOpenAIRequest(request, model);
                var content = new StringContent(JsonSerializer.Serialize(azureRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var openAIResponse = JsonSerializer.Deserialize<AzureOpenAIResponse>(responseContent);
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return new ModelResponse
                {
                    Content = openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                    Model = model,
                    TotalTokens = openAIResponse?.Usage?.TotalTokens ?? 0,
                    ProcessingTimeMs = executionTime,
                    Metadata = new Dictionary<string, object>
                    {
                        ["finish_reason"] = openAIResponse?.Choices?.FirstOrDefault()?.FinishReason ?? "unknown",
                        ["usage"] = openAIResponse?.Usage
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Azure OpenAI request");
                throw;
            }
        }

        private Dictionary<string, object> CreateAzureOpenAIRequest(ModelRequest request, string model)
        {
            var messages = CreateMessages(request);
            var parameters = new Dictionary<string, object>(_defaultParameters);

            // Override with request-specific parameters
            if (request.Metadata.TryGetValue("temperature", out var temp))
                parameters["temperature"] = Convert.ToDouble(temp);
            if (request.Metadata.TryGetValue("max_tokens", out var maxTokens))
                parameters["max_tokens"] = Convert.ToInt32(maxTokens);

            // Add required fields
            parameters["model"] = model;
            parameters["messages"] = messages;

            return parameters;
        }

        private List<object> CreateMessages(ModelRequest request)
        {
            var messages = new List<object>();
            
            // Add system message if provided
            if (request.Metadata.TryGetValue("system_message", out var systemMessage) && !string.IsNullOrEmpty(systemMessage as string))
            {
                messages.Add(new { role = "system", content = systemMessage });
            }

            // Add user message
            messages.Add(new { role = "user", content = request.Input });

            return messages;
        }

        private class AzureOpenAIResponse
        {
            public string Id { get; set; } = string.Empty;
            public string Object { get; set; } = string.Empty;
            public long Created { get; set; }
            public string Model { get; set; } = string.Empty;
            public List<AzureOpenAIChoice> Choices { get; set; } = new List<AzureOpenAIChoice>();
            public AzureOpenAIUsage Usage { get; set; } = new AzureOpenAIUsage();
        }

        private class AzureOpenAIChoice
        {
            public int Index { get; set; }
            public AzureOpenAIMessage Message { get; set; } = new AzureOpenAIMessage();
            public string FinishReason { get; set; } = string.Empty;
        }

        private class AzureOpenAIMessage
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        private class AzureOpenAIUsage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
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
            var model = request.Metadata.TryGetValue("model", out var modelObj) ? modelObj as string : "gpt-35-turbo";
            var availableModels = await GetAvailableModelsAsync();
            if (!availableModels.Any(m => m.Id == model))
            {
                result.IsValid = false;
                result.Errors.Add($"Model {model} is not available");
            }

            return result;
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                // No direct health endpoint; try a simple completions call with a short prompt
                var testRequest = new
                {
                    prompt = "ping",
                    max_tokens = 1
                };
                var url = $"openai/deployments/gpt-35-turbo/completions?api-version={_apiVersion}";
                var content = new StringContent(JsonSerializer.Serialize(testRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
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
                _logger.LogError(ex, "Error checking Azure OpenAI health status");
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



        /// <summary>
        /// Azure OpenAI model implementation.
        /// </summary>
        private class AzureOpenAIModel : IModel
        {
            private readonly string _modelName;
            private readonly HttpClient _httpClient;
            private readonly ILogger _logger;
            private readonly string _apiVersion;
            private ModelInfo _info;

            public AzureOpenAIModel(string modelName, HttpClient httpClient, ILogger logger, string apiVersion)
            {
                _modelName = modelName;
                _httpClient = httpClient;
                _logger = logger;
                _apiVersion = apiVersion;
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
                            Description = "Azure OpenAI " + _modelName + " model",
                            Version = _apiVersion,
                            Type = ModelType.TextGeneration,
                            Provider = "azure-openai",
                            IsAvailable = true,
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                    return _info;
                }
            }

            public bool IsLoaded => true;

            public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("Executing Azure OpenAI request with model {Model}", _modelName);
                try
                {
                    var startTime = DateTime.UtcNow;
                    var url = $"openai/deployments/{_modelName}/chat/completions?api-version={_apiVersion}";
                    var azureRequest = CreateAzureOpenAIRequest(request, _modelName);
                    var content = new StringContent(JsonSerializer.Serialize(azureRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var openAIResponse = JsonSerializer.Deserialize<AzureOpenAIResponse>(responseContent);
                    var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return new ModelResponse
                    {
                        Content = openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                        Model = _modelName,
                        TotalTokens = openAIResponse?.Usage?.TotalTokens ?? 0,
                        ProcessingTimeMs = executionTime,
                        Metadata = new Dictionary<string, object>
                        {
                            ["finish_reason"] = openAIResponse?.Choices?.FirstOrDefault()?.FinishReason ?? "unknown",
                            ["usage"] = openAIResponse?.Usage
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing Azure OpenAI request");
                    throw;
                }
            }

            private Dictionary<string, object> CreateAzureOpenAIRequest(ModelRequest request, string model)
            {
                var messages = new List<object>();
                if (request.Metadata.TryGetValue("system_message", out var systemMessage) && !string.IsNullOrEmpty(systemMessage as string))
                {
                    messages.Add(new { role = "system", content = systemMessage });
                }
                messages.Add(new { role = "user", content = request.Input });

                var parameters = new Dictionary<string, object>();
                if (request.Metadata.TryGetValue("temperature", out var temp))
                    parameters["temperature"] = Convert.ToDouble(temp);
                if (request.Metadata.TryGetValue("max_tokens", out var maxTokens))
                    parameters["max_tokens"] = Convert.ToInt32(maxTokens);

                parameters["model"] = model;
                parameters["messages"] = messages;

                return parameters;
            }

            public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException("Streaming not implemented yet");
            }

            public ModelCapabilities GetCapabilities()
            {
                return new ModelCapabilities
                {
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsTextEmbedding = true,
                    MaxInputLength = 128000,
                    MaxOutputLength = 128000,
                    SupportedLanguages = new List<string> { "en", "es", "fr", "de", "it", "pt", "nl", "pl", "ru", "ja", "ko", "zh" }
                };
            }

            public Task UnloadAsync(CancellationToken cancellationToken = default)
            {
                // No cleanup needed for Azure OpenAI models
                return Task.CompletedTask;
            }

        }
    }
} 