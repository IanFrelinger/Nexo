using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.AI.LlamaNative.Providers;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Orchestrator for Llama native provider that delegates to specialized providers.
    /// </summary>
    public class LlamaNativeProvider : ILlamaProvider, IModelProvider, IAIProvider
    {
        private readonly ILogger<LlamaNativeProvider> _logger;
        private readonly ModelManager _modelManager;
        private readonly TextGenerationProvider _textGenerationProvider;
        private readonly CodeGenerationProvider _codeGenerationProvider;
        private readonly ChatProvider _chatProvider;
        private readonly GpuAccelerationProvider _gpuProvider;
        private bool _isInitialized = false;

        public string ProviderId => "llama-native";
        public string DisplayName => "LLama Native";
        public string Name => "LLama Native";
        public string Description => "Native LLama implementation using LlamaSharp for offline AI operations";
        public string Version => "1.0.0";
        public AIProviderType ProviderType => AIProviderType.LlamaNative;
        public int Priority => 90;
        public bool IsOfflineCapable => true;
        public bool SupportsGpuAcceleration => _gpuProvider.DetectGpuSupport();
        public bool SupportsStreaming => true;
        public int MaxContextLength => 4096;
        public string ModelsPath => _modelManager.ModelsDirectory;

        public IEnumerable<string> SupportedModelTypes => new[]
        {
            "TextGeneration",
            "CodeGeneration",
            "Chat"
        };

        public LlamaNativeProvider(ILogger<LlamaNativeProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelManager = new ModelManager(logger);
            _textGenerationProvider = new TextGenerationProvider(logger);
            _codeGenerationProvider = new CodeGenerationProvider(logger);
            _chatProvider = new ChatProvider(logger);
            _gpuProvider = new GpuAccelerationProvider(logger);
        }

        public bool IsModelLoaded(string modelName)
        {
            return _modelManager.IsModelLoaded(modelName);
        }

        public async Task<bool> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Loading Llama model: {ModelName}", modelName);
                return await _modelManager.LoadModelAsync(modelName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model {ModelName}", modelName);
                return false;
            }
        }

        public async Task<bool> UnloadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Unloading Llama model: {ModelName}", modelName);
                return await _modelManager.UnloadModelAsync(modelName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading model {ModelName}", modelName);
                return false;
            }
        }

        public async Task<ModelResponse> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating text with Llama");
                return await _textGenerationProvider.GenerateTextAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return new ModelResponse
                {
                    Success = false,
                    Error = $"Text generation failed: {ex.Message}"
                };
            }
        }

        public async Task<ModelResponse> GenerateCodeAsync(CodeGenerationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating code with Llama");
                return await _codeGenerationProvider.GenerateCodeAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code");
                return new ModelResponse
                {
                    Success = false,
                    Error = $"Code generation failed: {ex.Message}"
                };
            }
        }

        public async Task<ChatCompletionResponse> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Completing chat with Llama");
                return await _chatProvider.CompleteChatAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing chat");
                return new ChatCompletionResponse
                {
                    Success = false,
                    Error = $"Chat completion failed: {ex.Message}"
                };
            }
        }

        public async Task<ModelListResponse> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Listing Llama models");
                return await _modelManager.ListModelsAsync(cancellationToken);
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

        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_isInitialized)
                    return true;

                _logger.LogInformation("Initializing Llama native provider");
                
                await _modelManager.InitializeAsync(cancellationToken);
                await _gpuProvider.InitializeAsync(cancellationToken);
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Llama native provider");
                return false;
            }
        }
    }
}
