using System.Net.Http;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Services.AI.Ollama;
using ModelInfo = Nexo.Core.Domain.Entities.AI.ModelInfo;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Orchestrates Ollama provider operations using specialized services.
    /// </summary>
    public partial class OllamaProvider : ILlamaProvider, Nexo.Core.Application.Interfaces.AI.IModelProvider, Nexo.Core.Application.Interfaces.AI.IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaProvider> _logger;
        private readonly ModelManager _modelManager;
        private readonly RequestProcessor _requestProcessor;
        private readonly CapabilityProvider _capabilityProvider;
        private readonly PerformanceEstimator _performanceEstimator;

        public OllamaProvider(
            HttpClient httpClient,
            ILogger<OllamaProvider> logger,
            string baseUrl = "http://localhost:11434")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nexo-LLama-Provider/1.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Longer timeout for model operations

            // Initialize specialized services
            _modelManager = new ModelManager(_httpClient, _logger);
            _requestProcessor = new RequestProcessor(_httpClient, _logger);
            _capabilityProvider = new CapabilityProvider();
            _performanceEstimator = new PerformanceEstimator();
        }

        // ILlamaProvider implementation
        public string ProviderId => _capabilityProvider.ProviderId;
        public string DisplayName => _capabilityProvider.DisplayName;
        public string Description => _capabilityProvider.Description;
        public int Priority => _capabilityProvider.Priority;
        public bool IsOfflineCapable => _capabilityProvider.IsOfflineCapable;
        public bool SupportsGpuAcceleration => _capabilityProvider.SupportsGpuAcceleration;
        public bool SupportsStreaming => _capabilityProvider.SupportsStreaming;
        public int MaxContextLength => _capabilityProvider.MaxContextLength;
        public string ModelsPath => _capabilityProvider.ModelsPath;

        public IEnumerable<string> SupportedModelTypes => _capabilityProvider.SupportedModelTypes;

        public bool IsModelLoaded(string modelName) => _modelManager.IsModelLoaded(modelName);

        public async Task LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await _modelManager.LoadModelAsync(modelName, cancellationToken);
        }

        public async Task UnloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await _modelManager.UnloadModelAsync(modelName, cancellationToken);
        }

        public async Task<long> GetModelMemoryUsageAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return await _modelManager.GetModelMemoryUsageAsync(modelName, cancellationToken);
        }

        public async Task<ModelInfo> DownloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return await _modelManager.DownloadModelAsync(modelName, cancellationToken);
        }

        public async Task<bool> RemoveModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return await _modelManager.RemoveModelAsync(modelName, cancellationToken);
        }

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsForDownloadAsync(CancellationToken cancellationToken = default)
        {
            // For Ollama, available models are the same as downloaded models
            return await GetAvailableModelsAsync(cancellationToken);
        }

        // IModelProvider implementation
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelManager.GetAvailableModelsAsync(cancellationToken);
        }

        public async Task<IModel> LoadModelForProviderAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await LoadModelAsync(modelName, cancellationToken);
            return new OllamaModel(modelName, _httpClient, _logger);
        }

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            return await _requestProcessor.ExecuteAsync(request, cancellationToken);
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            return await _requestProcessor.GetHealthStatusAsync(cancellationToken);
        }

        // IModelProvider implementation
        public AIProviderType ProviderType => _capabilityProvider.ProviderType;
        public string Name => _capabilityProvider.Name;
        public string Version => _capabilityProvider.Version;

        public bool IsAvailable() => _capabilityProvider.IsAvailable();

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

        public bool IsModelCompatible(ModelInfo model) => _capabilityProvider.IsModelCompatible(model);

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
        public AIProviderCapabilities Capabilities => _capabilityProvider.Capabilities;
        public AIProviderStatus Status => _capabilityProvider.Status;

        public bool SupportsPlatform(PlatformType platform) => _capabilityProvider.SupportsPlatform(platform);

        public bool MeetsRequirements(AIRequirements requirements) => _capabilityProvider.MeetsRequirements(requirements);

        public bool HasRequiredResources(AIResources resources) => _capabilityProvider.HasRequiredResources(resources);

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

        public async Task<PerformanceEstimate> EstimatePerformanceAsync(AIOperationContext context)
        {
            return await _performanceEstimator.EstimatePerformanceAsync(context);
        }

        public bool SupportsEngineType(AIEngineType engineType) => _capabilityProvider.SupportsEngineType(engineType);

        public AIEngineType EngineType => _capabilityProvider.EngineType;
        public AIProviderType Provider => _capabilityProvider.Provider;

        // IAIProvider.DownloadModelAsync implementation
        async Task<ModelInfo> IAIProvider.DownloadModelAsync(string modelId, string variantId)
        {
            return await DownloadModelForAIProviderAsync(modelId, variantId);
        }
    }

}
