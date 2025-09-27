using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.AI.AzureOpenAI.Providers;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Orchestrator for Azure OpenAI model provider that delegates to specialized providers.
    /// </summary>
    public partial class AzureOpenAiModelProvider : IModelProvider
    {
        private readonly ILogger<AzureOpenAiModelProvider> _logger;
        private readonly TextGenerationProvider _textGenerationProvider;
        private readonly CodeGenerationProvider _codeGenerationProvider;
        private readonly EmbeddingProvider _embeddingProvider;
        private readonly ChatCompletionProvider _chatCompletionProvider;
        private readonly ModelManagementProvider _modelManagementProvider;

        public AzureOpenAiModelProvider(ILogger<AzureOpenAiModelProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _textGenerationProvider = new TextGenerationProvider(logger);
            _codeGenerationProvider = new CodeGenerationProvider(logger);
            _embeddingProvider = new EmbeddingProvider(logger);
            _chatCompletionProvider = new ChatCompletionProvider(logger);
            _modelManagementProvider = new ModelManagementProvider(logger);
        }

        public async Task<ModelResponse> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating text with Azure OpenAI");
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
                _logger.LogInformation("Generating code with Azure OpenAI");
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

        public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating embeddings with Azure OpenAI");
                return await _embeddingProvider.GenerateEmbeddingsAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                return new EmbeddingResponse
                {
                    Success = false,
                    Error = $"Embedding generation failed: {ex.Message}"
                };
            }
        }

        public async Task<ChatCompletionResponse> CompleteChatAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Completing chat with Azure OpenAI");
                return await _chatCompletionProvider.CompleteChatAsync(request, cancellationToken);
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
                _logger.LogInformation("Listing models with Azure OpenAI");
                return await _modelManagementProvider.ListModelsAsync(cancellationToken);
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
    }
}
