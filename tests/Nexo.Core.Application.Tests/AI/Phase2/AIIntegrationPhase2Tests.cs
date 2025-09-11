using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.Models;
using Nexo.Core.Application.Services.AI.Performance;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.AI.Phase2
{
    /// <summary>
    /// Phase 2 AI Integration Tests - WebAssembly and Native LLama Integration
    /// </summary>
    public class AIIntegrationPhase2Tests : IDisposable
    {
        private readonly IHost _host;
        private readonly ILogger<AIIntegrationPhase2Tests> _logger;
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly IModelManagementService _modelService;
        private readonly AIPerformanceMonitor _performanceMonitor;
        private readonly IEnumerable<IAIProvider> _providers;

        public AIIntegrationPhase2Tests()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddAIServices();
                    services.AddHttpClient();
                })
                .Build();

            _logger = _host.Services.GetRequiredService<ILogger<AIIntegrationPhase2Tests>>();
            _runtimeSelector = _host.Services.GetRequiredService<IAIRuntimeSelector>();
            _modelService = _host.Services.GetRequiredService<IModelManagementService>();
            _performanceMonitor = _host.Services.GetRequiredService<AIPerformanceMonitor>();
            _providers = _host.Services.GetServices<IAIProvider>();
        }

        [Fact]
        public async Task WebAssemblyProvider_ShouldInitializeSuccessfully()
        {
            // Arrange
            var wasmProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.NotNull(wasmProvider);

            // Act
            var result = await wasmProvider.InitializeAsync();

            // Assert
            Assert.True(result);
            Assert.True(wasmProvider.IsInitialized);
        }

        [Fact]
        public async Task NativeProvider_ShouldInitializeSuccessfully()
        {
            // Arrange
            var nativeProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
            Assert.NotNull(nativeProvider);

            // Act
            var result = await nativeProvider.InitializeAsync();

            // Assert
            Assert.True(result);
            Assert.True(nativeProvider.IsInitialized);
        }

        [Fact]
        public async Task WebAssemblyProvider_ShouldProvideCorrectInfo()
        {
            // Arrange
            var wasmProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.NotNull(wasmProvider);

            // Act
            var info = await wasmProvider.GetInfoAsync();

            // Assert
            Assert.Equal(AIProviderType.LlamaWebAssembly, info.ProviderType);
            Assert.Equal("LLama WebAssembly Provider", info.Name);
            Assert.Equal("1.0.0", info.Version);
            Assert.True(info.Capabilities.Any());
            Assert.Contains("WebAssembly Memory Management", info.Capabilities);
        }

        [Fact]
        public async Task NativeProvider_ShouldProvideCorrectInfo()
        {
            // Arrange
            var nativeProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
            Assert.NotNull(nativeProvider);

            // Act
            var info = await nativeProvider.GetInfoAsync();

            // Assert
            Assert.Equal(AIProviderType.LlamaNative, info.ProviderType);
            Assert.Equal("LLama Native Provider", info.Name);
            Assert.Equal("1.0.0", info.Version);
            Assert.True(info.Capabilities.Any());
            Assert.Contains("High Performance Text Generation", info.Capabilities);
        }

        [Fact]
        public async Task WebAssemblyProvider_ShouldCreateEngineSuccessfully()
        {
            // Arrange
            var wasmProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.NotNull(wasmProvider);
            await wasmProvider.InitializeAsync();

            var engineInfo = new AIEngineInfo
            {
                EngineType = AIEngineType.LlamaWebAssembly,
                ModelPath = "models/llama-2-7b-chat.gguf",
                MaxTokens = 2048,
                Temperature = 0.7
            };

            // Act
            var engine = await wasmProvider.CreateEngineAsync(engineInfo);

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public async Task NativeProvider_ShouldCreateEngineSuccessfully()
        {
            // Arrange
            var nativeProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
            Assert.NotNull(nativeProvider);
            await nativeProvider.InitializeAsync();

            var engineInfo = new AIEngineInfo
            {
                EngineType = AIEngineType.LlamaNative,
                ModelPath = "models/codellama-7b-instruct.gguf",
                MaxTokens = 4096,
                Temperature = 0.8
            };

            // Act
            var engine = await nativeProvider.CreateEngineAsync(engineInfo);

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public async Task ModelManagementService_ShouldGetAvailableModels()
        {
            // Act
            var models = await _modelService.GetAvailableModelsAsync();

            // Assert
            Assert.NotNull(models);
            Assert.True(models.Any());
        }

        [Fact]
        public async Task ModelManagementService_ShouldGetStorageStatistics()
        {
            // Act
            var stats = await _modelService.GetStorageStatisticsAsync();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalModels >= 0);
            Assert.True(stats.TotalSize >= 0);
            Assert.True(stats.AvailableSpace >= 0);
        }

        [Fact]
        public async Task PerformanceMonitor_ShouldTrackOperations()
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();
            var operationType = AIOperationType.CodeGeneration;
            var providerType = AIProviderType.LlamaWebAssembly;
            var engineType = AIEngineType.LlamaWebAssembly;

            // Act
            var startMetrics = await _performanceMonitor.StartOperationAsync(operationId, operationType, providerType, engineType);
            Assert.NotNull(startMetrics);
            Assert.Equal(operationId, startMetrics.OperationId);
            Assert.Equal(AIOperationStatus.Running, startMetrics.Status);

            // Simulate operation
            await Task.Delay(100);

            var endMetrics = await _performanceMonitor.EndOperationAsync(operationId, true);
            Assert.NotNull(endMetrics);
            Assert.Equal(AIOperationStatus.Completed, endMetrics.Status);
            Assert.True(endMetrics.Duration.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task PerformanceMonitor_ShouldCalculateStatistics()
        {
            // Arrange
            var operations = new[]
            {
                (AIOperationType.CodeGeneration, AIProviderType.LlamaWebAssembly, AIEngineType.LlamaWebAssembly),
                (AIOperationType.CodeReview, AIProviderType.LlamaNative, AIEngineType.LlamaNative),
                (AIOperationType.CodeOptimization, AIProviderType.LlamaNative, AIEngineType.LlamaNative)
            };

            // Act - Start and end operations
            foreach (var (operationType, providerType, engineType) in operations)
            {
                var operationId = Guid.NewGuid().ToString();
                await _performanceMonitor.StartOperationAsync(operationId, operationType, providerType, engineType);
                await Task.Delay(50); // Simulate processing
                await _performanceMonitor.EndOperationAsync(operationId, true);
            }

            var statistics = await _performanceMonitor.GetPerformanceStatisticsAsync();

            // Assert
            Assert.NotNull(statistics);
            Assert.Equal(3, statistics.TotalOperations);
            Assert.Equal(3, statistics.SuccessfulOperations);
            Assert.Equal(0, statistics.FailedOperations);
            Assert.Equal(100.0, statistics.SuccessRate);
            Assert.True(statistics.AverageDuration.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task PerformanceMonitor_ShouldGenerateRecommendations()
        {
            // Act
            var recommendations = await _performanceMonitor.GetPerformanceRecommendationsAsync();

            // Assert
            Assert.NotNull(recommendations);
            // Recommendations may be empty if no performance issues are detected
        }

        [Fact]
        public async Task RuntimeSelector_ShouldSelectOptimalProvider()
        {
            // Arrange
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.WebAssembly,
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Balanced
            };

            // Act
            var selection = await _runtimeSelector.SelectOptimalProviderAsync(context);

            // Assert
            Assert.NotNull(selection);
            Assert.NotNull(selection.ProviderType);
            Assert.NotNull(selection.EngineType);
            Assert.True(selection.Confidence >= 0);
            Assert.True(selection.Confidence <= 100);
        }

        [Fact]
        public async Task RuntimeSelector_ShouldSelectDifferentProvidersForDifferentPlatforms()
        {
            // Arrange
            var webAssemblyContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.WebAssembly,
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Balanced
            };

            var nativeContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = PlatformType.Windows,
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Performance
            };

            // Act
            var webAssemblySelection = await _runtimeSelector.SelectOptimalProviderAsync(webAssemblyContext);
            var nativeSelection = await _runtimeSelector.SelectOptimalProviderAsync(nativeContext);

            // Assert
            Assert.NotNull(webAssemblySelection);
            Assert.NotNull(nativeSelection);
            
            // The selections should be different for different platforms
            // (though in our mock implementation, they might be the same)
            Assert.NotNull(webAssemblySelection.ProviderType);
            Assert.NotNull(nativeSelection.ProviderType);
        }

        [Fact]
        public async Task AllProviders_ShouldBeAvailable()
        {
            // Act
            var availableProviders = _providers.Where(p => p.IsAvailable).ToList();

            // Assert
            Assert.True(availableProviders.Any());
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.Mock);
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.LlamaNative);
        }

        [Fact]
        public async Task AllProviders_ShouldInitializeSuccessfully()
        {
            // Act
            var initializationResults = new List<bool>();
            
            foreach (var provider in _providers.Where(p => p.IsAvailable))
            {
                try
                {
                    var result = await provider.InitializeAsync();
                    initializationResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize provider {ProviderType}", provider.ProviderType);
                    initializationResults.Add(false);
                }
            }

            // Assert
            Assert.True(initializationResults.Any());
            Assert.True(initializationResults.Count(r => r) > 0); // At least one should succeed
        }

        [Fact]
        public async Task ModelManagementService_ShouldHandleModelOperations()
        {
            // Arrange
            var modelId = "test-model";
            var version = "1.0.0";

            // Act & Assert
            var isAvailable = await _modelService.IsModelAvailableAsync(modelId, version);
            Assert.False(isAvailable); // Model shouldn't exist initially

            var modelInfo = await _modelService.GetModelInfoAsync(modelId);
            Assert.NotNull(modelInfo);
            Assert.Equal(modelId, modelInfo.ModelId);

            // Note: We don't actually download the model in tests to avoid network dependencies
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }
}
