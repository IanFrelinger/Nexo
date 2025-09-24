using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Services.AI.Mock.Providers;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Orchestrates mock AI model operations using specialized providers.
    /// </summary>
    public class MockModelProvider : IModelProvider
    {
        private readonly ILogger<MockModelProvider> _logger;
        private readonly MockTextGenerationProvider _textGenerationProvider;
        private readonly MockCodeGenerationProvider _codeGenerationProvider;
        private readonly MockEmbeddingProvider _embeddingProvider;
        private readonly MockChatCompletionProvider _chatCompletionProvider;
        private readonly MockModelManagementProvider _modelManagementProvider;

        public string ProviderId => "mock";
        public string DisplayName => "Mock Provider";
        public bool RequiresOnline => false;
        public bool IsAvailable => true;

        public MockModelProvider(ILogger<MockModelProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _textGenerationProvider = new MockTextGenerationProvider(logger);
            _codeGenerationProvider = new MockCodeGenerationProvider(logger);
            _embeddingProvider = new MockEmbeddingProvider(logger);
            _chatCompletionProvider = new MockChatCompletionProvider(logger);
            _modelManagementProvider = new MockModelManagementProvider(logger);
        }

        public async Task<TextGenerationResult> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken = default)
        {
            return await _textGenerationProvider.GenerateTextAsync(request, cancellationToken);
        }

        public async Task<CodeGenerationResult> GenerateCodeAsync(CodeGenerationRequest request, CancellationToken cancellationToken = default)
        {
            return await _codeGenerationProvider.GenerateCodeAsync(request, cancellationToken);
        }

        public async Task<EmbeddingResult> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            return await _embeddingProvider.GenerateEmbeddingsAsync(request, cancellationToken);
        }

        public async Task<ChatCompletionResult> GetChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            return await _chatCompletionProvider.GetChatCompletionAsync(request, cancellationToken);
        }

        public async Task<List<ModelInfo>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelManagementProvider.ListAvailableModelsAsync(cancellationToken);
        }

        public Task<bool> IsModelSupportedAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return _modelManagementProvider.IsModelSupportedAsync(modelName, cancellationToken);
        }

        public Task<ModelPerformanceMetrics> GetModelPerformanceMetricsAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return _modelManagementProvider.GetModelPerformanceMetricsAsync(modelName, cancellationToken);
        }

        public Task<ModelUsageMetrics> GetModelUsageMetricsAsync(string modelName, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
        {
            return _modelManagementProvider.GetModelUsageMetricsAsync(modelName, startTime, endTime, cancellationToken);
        }

        public Task<ModelHealthStatus> GetModelHealthStatusAsync(string modelName, CancellationToken cancellationToken = default)
        {
            return _modelManagementProvider.GetModelHealthStatusAsync(modelName, cancellationToken);
        }
    }
}
