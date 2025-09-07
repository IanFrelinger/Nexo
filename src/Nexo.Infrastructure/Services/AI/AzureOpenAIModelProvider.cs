using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    /// Represents a provider for Azure-hosted OpenAI models, supporting tasks such as text generation,
    /// code generation, and text embedding. This implementation provides integration with Azure OpenAI services.
    /// </summary>
    public class AzureOpenAiModelProvider : IModelProvider
    {
        /// <summary>
        /// An instance of <see cref="HttpClient"/> used to make HTTP requests to the Azure OpenAI service.
        /// </summary>
        /// <remarks>
        /// This client is configured with a base address set to the Azure OpenAI endpoint and includes default headers
        /// such as "api-key" for authentication and a custom "User-Agent" string.
        /// </remarks>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// A logger instance used for recording diagnostic messages, warnings, and errors
        /// related to the operations and functionality of the <c>AzureOpenAiModelProvider</c> class.
        /// </summary>
        private readonly ILogger<AzureOpenAiModelProvider> _logger;

        /// <summary>
        /// The API key used for authenticating requests to the Azure OpenAI service.
        /// This key is required to access and interact with the service.
        /// </summary>
        private readonly string _apiKey;

        /// <summary>
        /// Represents the endpoint URL for connecting to the Azure OpenAI service.
        /// </summary>
        /// <remarks>
        /// The endpoint is utilized as the base address for HTTP client requests to the Azure OpenAI service.
        /// It must be a valid URI and is trimmed of any trailing slashes when initialized.
        /// </remarks>
        private readonly string _endpoint;

        /// <summary>
        /// Represents the API version used by the Azure OpenAI service.
        /// This value configures the specific version of the API that the client interacts with,
        /// ensuring compatibility with features and endpoints available in that version.
        /// </summary>
        private readonly string _apiVersion;

        /// <summary>
        /// Stores a collection of default parameter values used for configuring Azure OpenAI API requests.
        /// </summary>
        /// <remarks>
        /// This dictionary includes pre-defined settings such as "temperature", "max_tokens", "top_p",
        /// "frequency_penalty", and "presence_penalty", which influence the behavior of the AI model.
        /// These defaults can be overridden by request-specific parameters when generating API calls.
        /// </remarks>
        private readonly Dictionary<string, object> _defaultParameters;

        /// Represents a provider for interacting with Azure-hosted OpenAI GPT models.
        /// This class implements the IModelProvider interface and provides functionality
        /// to interact with Azure's OpenAI services for tasks like text generation and chat.
        public AzureOpenAiModelProvider(
            HttpClient httpClient,
            ILogger<AzureOpenAiModelProvider> logger,
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

        /// <summary>
        /// Gets the unique identifier for the provider.
        /// This identifier is used to distinguish the specific provider implementation
        /// in contexts where multiple providers may be registered or managed.
        /// </summary>
        public string ProviderId => "azure-openai";

        /// <summary>
        /// Gets the display name of the Azure OpenAI Model Provider.
        /// </summary>
        /// <remarks>
        /// This property provides a human-readable identifier for the Azure OpenAI service,
        /// which can be used for logging or display purposes. In this implementation, it returns "Azure OpenAI".
        /// </remarks>
        public string DisplayName => "Azure OpenAI";

        /// <summary>
        /// Provides a textual description of the Azure OpenAI model provider,
        /// indicating its capabilities and purpose.
        /// </summary>
        public string Description => "Azure-hosted OpenAI GPT models for text generation and chat";

        /// <summary>
        /// Gets a value indicating whether the Azure OpenAI model provider is available for use.
        /// </summary>
        /// <remarks>
        /// The provider is considered available if both the API key and endpoint are non-empty strings.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the provider is available; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_endpoint);
        
        // IModelProvider interface properties
        /// Gets the name of the model provider.
        /// This property returns the display name associated with the model provider,
        /// which serves as a user-friendly identifier for the provider.
        public string Name => DisplayName;

        /// <summary>
        /// Gets the type of the provider. This property identifies the specific provider
        /// type as "AzureOpenAI". It is used to differentiate this provider
        /// from other implementations of IModelProvider.
        /// </summary>
        public string ProviderType => "AzureOpenAI";

        /// <summary>
        /// Determines whether the model provider is enabled and available for use.
        /// </summary>
        /// <remarks>
        /// This property evaluates the underlying availability of the provider by checking
        /// if the required configuration, such as API key and endpoint, are properly set.
        /// It returns true if the provider is available; otherwise, false.
        /// </remarks>
        public bool IsEnabled => IsAvailable;

        /// <summary>
        /// Indicates whether the current model provider is the primary provider in use.
        /// </summary>
        /// <remarks>
        /// This property returns a boolean value that specifies if this provider is
        /// designated as the primary provider among multiple model providers.
        /// </remarks>
        public bool IsPrimary => false;

        /// <summary>
        /// Gets the collection of supported model types offered by the Azure OpenAI Model Provider.
        /// </summary>
        /// <remarks>
        /// The property returns an enumeration of model types including TextGeneration,
        /// CodeGeneration, and TextEmbedding. These values indicate the categories
        /// of AI models that this provider supports.
        /// </remarks>
        public IEnumerable<ModelType> SupportedModelTypes =>
        [
            ModelType.TextGeneration,
            ModelType.CodeGeneration,
            ModelType.TextEmbedding
        ];

        /// Defines the capabilities of the Azure OpenAI model provider.
        /// This includes the features supported by the model, such as
        /// streaming, function calling, text embedding, and the constraints
        /// like maximum input and output lengths, as well as supported languages.
        public ModelCapabilities Capabilities => new ModelCapabilities
        {
            SupportsTextGeneration = true,
            SupportsCodeGeneration = true,
            SupportsAnalysis = true,
            SupportsOptimization = false,
            SupportsStreaming = true
        };

        /// <summary>
        /// Retrieves a list of available models from the Azure OpenAI service.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <c>ModelInfo</c> objects representing the available models.</returns>
        public Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            // Azure OpenAI does not have a public models endpoint; assume deployments are known/configured.
            // For demo, return a static list or fetch from a config/service if available.
            // In production, you may want to load from configuration or a management API.
            return Task.FromResult<IEnumerable<ModelInfo>>(new List<ModelInfo>
            {
                new ModelInfo
                {
                    Name = "gpt-35-turbo",
                    DisplayName = "gpt-35-turbo",
                    ModelType = ModelType.TextGeneration,
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
                },
                new ModelInfo
                {
                    Name = "gpt-4",
                    DisplayName = "gpt-4",
                    ModelType = ModelType.TextGeneration,
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
                }
            });
        }

        /// <summary>
        /// Asynchronously loads an AI model based on the specified model name.
        /// </summary>
        /// <param name="modelName">The name of the model to be loaded.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the loaded model instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified model is not found or unavailable.</exception>
        public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                throw new InvalidOperationException($"Model {modelName} not found or not available");
            }
            return new AzureOpenAIModel(modelName, _httpClient, _logger, _apiVersion);
        }

        /// <summary>
        /// Retrieves model information for the specified model name.
        /// </summary>
        /// <param name="modelName">The name or identifier of the model to retrieve information for.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the model information for the given model name, or null if the model is not found.</returns>
        public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default)
        {
            var models = await GetAvailableModelsAsync(cancellationToken);
            return models.FirstOrDefault(m => m.Name == modelName);
        }

        /// <summary>
        /// Validates the specified model asynchronously by checking its availability and information.
        /// This method attempts to retrieve the model information and validates if the model
        /// exists and is properly configured. If the model cannot be found or an error occurs
        /// during validation, the operation returns validation errors in the result.
        /// Any exceptions or errors encountered during the validation process are caught and
        /// added as validation errors in the response.
        /// <param name="modelName">The name of the model to validate.</param>
        /// <param name="cancellationToken">A cancellation token to observe during the task.</param>
        /// <returns>Returns a <see cref="ModelValidationResult"/> indicating whether the model is valid,
        /// along with any validation errors, warnings, or additional information.</returns>
        public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Executes an asynchronous operation to process a given <see cref="ModelRequest"/>
        /// and returns a response in the form of <see cref="ModelResponse"/>.
        /// </summary>
        /// <param name="request">The request containing the parameters and data needed for the operation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing a <see cref="ModelResponse"/> with the result of the execution.</returns>
        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing Azure OpenAI request");
            try
            {
                var startTime = DateTime.UtcNow;
                var model = request.Context?.TryGetValue("model", out var modelObj) == true ? modelObj as string : "gpt-35-turbo";
                var url = $"openai/deployments/{model}/chat/completions?api-version={_apiVersion}";
                var azureRequest = CreateAzureOpenAIRequest(request, model);
                var content = new StringContent(JsonSerializer.Serialize(azureRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var openAIResponse = JsonSerializer.Deserialize<AzureOpenAiResponse>(responseContent);
                var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return new ModelResponse
                {
                    Response = openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                    InputTokens = openAIResponse?.Usage?.PromptTokens ?? 0,
                    OutputTokens = openAIResponse?.Usage?.CompletionTokens ?? 0,
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

        /// <summary>
        /// Creates a request payload tailored for the Azure OpenAI API based on the provided model request and model name.
        /// This method constructs the necessary parameters and message structure for the Azure OpenAI API.
        /// </summary>
        /// <param name="request">The model request containing metadata and specific configuration for the request.</param>
        /// <param name="model">The name of the model to be used for the request, e.g., "gpt-35-turbo".</param>
        /// <returns>A dictionary containing the request parameters, including model information, messages, and configurable settings such as temperature and maximum tokens.</returns>
        private Dictionary<string, object> CreateAzureOpenAIRequest(ModelRequest request, string model)
        {
            var messages = CreateMessages(request);
            var parameters = new Dictionary<string, object>(_defaultParameters);

            // Override with request-specific parameters
            if (request.Context?.TryGetValue("temperature", out var temp) == true)
                parameters["temperature"] = Convert.ToDouble(temp);
            else
                parameters["temperature"] = request.Temperature;
            if (request.Context?.TryGetValue("max_tokens", out var maxTokens) == true)
                parameters["max_tokens"] = Convert.ToInt32(maxTokens);
            else
                parameters["max_tokens"] = request.MaxTokens;

            // Add required fields
            parameters["model"] = model;
            parameters["messages"] = messages;

            return parameters;
        }

        /// <summary>
        /// Creates a list of messages based on the provided <see cref="ModelRequest"/> instance.
        /// This includes system messages and user input for interaction with AI models.
        /// </summary>
        /// <param name="request">The model request containing the user input and optional metadata.</param>
        /// <returns>A list of message objects derived from the request, including system and user messages.</returns>
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

        /// <summary>
        /// Represents the response received from the Azure OpenAI API.
        /// </summary>
        private class AzureOpenAiResponse
        {
            /// <summary>
            /// Gets or sets the unique identifier of the Azure OpenAI response.
            /// </summary>
            public string Id { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the type of the object being returned in the response.
            /// </summary>
            /// <remarks>
            /// Represents metadata information about the object within a response,
            /// typically used to specify the type or category of the data being processed or returned.
            /// </remarks>
            public string Object { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the timestamp of when the response was created.
            /// </summary>
            /// <remarks>
            /// Represents the creation time of the response, typically provided as a Unix timestamp.
            /// This property is used to track when the response object was generated.
            /// </remarks>
            public long Created { get; set; }

            /// <summary>
            /// Represents the name or identifier of the model used in the Azure OpenAI response.
            /// </summary>
            /// <remarks>
            /// This property specifies the model type or version that was employed to generate a specific response.
            /// It is commonly used for tracking, diagnostics, or validation of model outputs in the Azure OpenAI service context.
            /// </remarks>
            public string Model { get; set; } = string.Empty;

            /// <summary>
            /// Represents a collection of individual choices returned by the Azure OpenAI response.
            /// Each choice contains details such as the generated message content, index,
            /// and the completion's finish reason.
            /// </summary>
            public List<AzureOpenAiChoice> Choices { get; set; } = new();

            /// <summary>
            /// Represents the usage details of the Azure OpenAI model interaction.
            /// </summary>
            /// <remarks>
            /// This property captures information about the token consumption during a request.
            /// It includes the number of tokens used for the prompt, the number of tokens generated in the completion,
            /// and the total number of tokens consumed in the interaction.
            /// </remarks>
            public AzureOpenAIUsage Usage { get; set; } = new();
        }

        /// <summary>
        /// Represents a choice returned by Azure OpenAI API during a request response.
        /// </summary>
        private class AzureOpenAiChoice
        {
            /// <summary>
            /// Gets or sets the index indicating the position or order of the choice
            /// in the response. This is typically used to track and identify
            /// individual responses within a collection of choices.
            /// </summary>
            public int Index { get; set; }

            /// <summary>
            /// Represents a message in the Azure OpenAI response.
            /// </summary>
            /// <remarks>
            /// The Message property typically encapsulates the role of the speaker and the
            /// content of the conversation as part of a response from the Azure OpenAI service.
            /// </remarks>
            public AzureOpenAiMessage Message { get; set; } = new AzureOpenAiMessage();

            /// <summary>
            /// Represents the reason why a response generation process was concluded.
            /// </summary>
            /// <remarks>
            /// This property typically indicates whether the generation was stopped due to reaching completion,
            /// hitting a token limit, encountering an error, or being explicitly interrupted. It provides
            /// context for understanding the termination state of the response generation.
            /// </remarks>
            public string FinishReason { get; set; } = string.Empty;
        }

        /// <summary>
        /// Represents a message used in Azure OpenAI interactions.
        /// </summary>
        private class AzureOpenAiMessage
        {
            /// <summary>
            /// Represents the role of the message within a conversation context.
            /// </summary>
            /// <remarks>
            /// This property is used to specify the role of the message, such as "system", "user", or "assistant".
            /// The role indicates the origin or purpose of the message within the interaction.
            /// </remarks>
            public string Role { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the content of the Azure OpenAI message.
            /// </summary>
            /// <remarks>
            /// This property holds the text content associated with a message in the Azure OpenAI response.
            /// It is used to capture the primary body of the message returned by the AI model.
            /// </remarks>
            public string Content { get; set; } = string.Empty;
        }

        /// <summary>
        /// Represents the token usage statistics for Azure OpenAI requests.
        /// </summary>
        private class AzureOpenAIUsage
        {
            /// <summary>
            /// Gets or sets the number of tokens used in the prompt portion of the request.
            /// </summary>
            public int PromptTokens { get; set; }

            /// <summary>
            /// Gets or sets the number of tokens consumed for the completion part of the operation.
            /// </summary>
            /// <remarks>
            /// Completion tokens represent the tokens generated as the result of a model's reasoning
            /// and response to a prompt. This value is a subset of the total tokens used in the interaction.
            /// </remarks>
            public int CompletionTokens { get; set; }

            /// <summary>
            /// Gets or sets the total number of tokens used in a request, including both the prompt tokens and completion tokens.
            /// </summary>
            /// <remarks>
            /// This property represents the combined number of tokens used for the input (prompt) and output (completion)
            /// of an Azure OpenAI request. It is often used to measure token consumption and monitor costs associated
            /// with API usage.
            /// </remarks>
            public int TotalTokens { get; set; }
        }

        /// <summary>
        /// Validates the specified model request to ensure all necessary fields are populated,
        /// and the requested model is available.
        /// </summary>
        /// <param name="request">The <see cref="ModelRequest"/> object containing details about the model invocation request.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, containing the <see cref="ModelValidationResult"/> with validation status and error messages, if any.</returns>
        public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
        {
            var result = new ModelValidationResult { IsValid = true };

            var errors = new List<string>();

            // Validate required fields
            if (string.IsNullOrEmpty(request.Input))
            {
                errors.Add("Input is required");
            }

            // Validate model
            var model = request.Context?.TryGetValue("model", out var modelObj) == true ? modelObj as string : "gpt-35-turbo";
            var availableModels = await GetAvailableModelsAsync();
            if (!availableModels.Any(m => m.Name == model))
            {
                errors.Add($"Model {model} is not available");
            }

            return new ModelValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Asynchronously checks the health status of the AI model provider by performing a lightweight test request.
        /// </summary>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="ModelHealthStatus"/> object containing the health status, including details like response time, error rate, and last error.</returns>
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
                    Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = responseTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Azure OpenAI health status");
                return new ModelHealthStatus
                {
                    Status = "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTimeMs = 0
                };
            }
        }


        /// <summary>
        /// Represents an Azure OpenAI model, providing the functionality to process requests and retrieve specific model capabilities.
        /// </summary>
        private class AzureOpenAIModel : IModel
        {
            /// <summary>
            /// The name of the Azure OpenAI model being used.
            /// This variable specifies the identifier of the model utilized for processing requests and generating outputs.
            /// </summary>
            private readonly string _modelName;

            /// <summary>
            /// Represents an instance of HttpClient used to send HTTP requests and receive HTTP responses
            /// from the Azure OpenAI API. This client is responsible for managing HTTP connections and
            /// facilitating communication with the API endpoint.
            /// </summary>
            private readonly HttpClient _httpClient;

            /// <summary>
            /// Logger instance used to log informational, warning, and error messages for the Azure OpenAI model processing.
            /// It helps to diagnose issues during model execution, monitor request execution time, and capture errors for debugging purposes.
            /// </summary>
            private readonly ILogger _logger;

            /// <summary>
            /// Represents the API version used for Azure OpenAI service requests.
            /// </summary>
            /// <remarks>
            /// This variable holds the specific version string required when interacting with Azure OpenAI endpoints,
            /// ensuring compatibility with the underlying API.
            /// </remarks>
            private readonly string _apiVersion;

            /// <summary>
            /// Stores information about the specific Azure OpenAI model, including metadata
            /// such as model name, version, provider, and capabilities. This variable is lazily
            /// initialized and provides details about the model when accessed through the getter.
            /// </summary>
            private ModelInfo? _info;

            /// <summary>
            /// Represents an Azure OpenAI model implementation.
            /// This class provides functionalities to interact with a specific AI model
            /// hosted on Azure OpenAI, supporting features such as processing requests
            /// and retrieving model capabilities.
            /// </summary>
            public AzureOpenAIModel(string modelName, HttpClient httpClient, ILogger logger, string apiVersion)
            {
                _modelName = modelName;
                _httpClient = httpClient;
                _logger = logger;
                _apiVersion = apiVersion;
            }

            /// <summary>
            /// Provides information about the Azure OpenAI model, including its ID, name, description, version, type,
            /// provider, availability status, and the last updated timestamp.
            /// </summary>
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

            /// <summary>
            /// Gets a value indicating whether the model is currently loaded and available for use.
            /// </summary>
            /// <remarks>
            /// This property returns true if the model is loaded and ready to process requests. Otherwise, it returns false.
            /// </remarks>
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

            /// <summary>
            /// Processes the given model request and returns a response generated by the Azure OpenAI model.
            /// </summary>
            /// <param name="request">The request object containing input data for the model.</param>
            /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
            /// <returns>A Task representing the asynchronous operation, containing the response generated by the Azure OpenAI model.</returns>
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
                    var openAiResponse = JsonSerializer.Deserialize<AzureOpenAiResponse>(responseContent);
                    var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                    return new ModelResponse
                    {
                        Response = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty,
                        InputTokens = openAiResponse?.Usage?.PromptTokens ?? 0,
                        OutputTokens = openAiResponse?.Usage?.CompletionTokens ?? 0,
                        ProcessingTimeMs = executionTime,
                        Metadata = new Dictionary<string, object>
                        {
                            ["finish_reason"] = openAiResponse?.Choices?.FirstOrDefault()?.FinishReason ?? "unknown",
                            ["usage"] = openAiResponse?.Usage
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing Azure OpenAI request");
                    throw;
                }
            }

            /// <summary>
            /// Creates a dictionary representing the request payload required for Azure OpenAI API.
            /// This method formats the user input, system messages, and additional parameters
            /// to construct the request in the appropriate format for the model.
            /// </summary>
            /// <param name="request">An object containing the user query and metadata, such as system messages or additional parameters.</param>
            /// <param name="model">The identifier for the specific model deployment to be used by the Azure OpenAI API.</param>
            /// <returns>A dictionary containing the necessary parameters to be sent in the API request, including model, messages, and other optional settings.</returns>
            private Dictionary<string, object> CreateAzureOpenAIRequest(ModelRequest request, string model)
            {
                var messages = new List<object>();
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                {
                    messages.Add(new { role = "system", content = request.SystemPrompt });
                }
                messages.Add(new { role = "user", content = request.Input });

                var parameters = new Dictionary<string, object>();
                if (request.Context?.TryGetValue("temperature", out var temp) == true)
                    parameters["temperature"] = Convert.ToDouble(temp);
                else
                    parameters["temperature"] = request.Temperature;
                if (request.Context?.TryGetValue("max_tokens", out var maxTokens) == true)
                    parameters["max_tokens"] = Convert.ToInt32(maxTokens);
                else
                    parameters["max_tokens"] = request.MaxTokens;

                parameters["model"] = model;
                parameters["messages"] = messages;

                return parameters;
            }

            /// <summary>
            /// Processes a stream of model responses based on the given request.
            /// </summary>
            /// <param name="request">The model request containing input, configuration, and parameters.</param>
            /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
            /// <returns>An enumerable of <see cref="ModelResponseChunk"/> representing chunks of the streaming model response.</returns>
            public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException("Streaming not implemented yet");
            }

            /// <summary>
            /// Retrieves the capabilities of the Azure OpenAI model, including features such as streaming,
            /// function calling, text embedding, supported languages, and input/output length constraints.
            /// </summary>
            /// <returns>
            /// A <see cref="ModelCapabilities"/> object that describes the supported features, including
            /// streaming, text embedding, and function calling support, maximum input/output lengths,
            /// and a list of supported languages.
            /// </returns>
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

            /// <summary>
            /// Unloads the model and releases any associated resources.
            /// </summary>
            /// <param name="cancellationToken">
            /// A CancellationToken that can be used to cancel the unload operation.
            /// </param>
            /// <returns>
            /// A Task representing the asynchronous unload operation.
            /// </returns>
            public Task LoadAsync(CancellationToken cancellationToken = default)
            {
                // Azure OpenAI models are loaded on demand
                return Task.CompletedTask;
            }

            public Task UnloadAsync(CancellationToken cancellationToken = default)
            {
                // No cleanup needed for Azure OpenAI models
                return Task.CompletedTask;
            }

        }
    }
} 