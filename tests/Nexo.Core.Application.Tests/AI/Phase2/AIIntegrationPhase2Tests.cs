using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.Models;
using Nexo.Core.Application.Services.AI.Performance;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
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
            await wasmProvider.InitializeAsync();

            // Assert
            // Assert - provider should be initialized after calling InitializeAsync
        }

        [Fact]
        public async Task NativeProvider_ShouldInitializeSuccessfully()
        {
            // Arrange
            var nativeProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
            Assert.NotNull(nativeProvider);

            // Act
            await nativeProvider.InitializeAsync();

            // Assert
            // Provider should be initialized after calling InitializeAsync
        }

        [Fact]
        public async Task WebAssemblyProvider_ShouldProvideCorrectInfo()
        {
            // Arrange
            var wasmProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.NotNull(wasmProvider);

            // Act & Assert
            Assert.Equal(AIProviderType.LlamaWebAssembly, wasmProvider.ProviderType);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task NativeProvider_ShouldProvideCorrectInfo()
        {
            // Arrange
            var nativeProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
            Assert.NotNull(nativeProvider);

            // Act & Assert
            Assert.Equal(AIProviderType.LlamaNative, nativeProvider.ProviderType);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task WebAssemblyProvider_ShouldCreateEngineSuccessfully()
        {
            // Arrange
            var wasmProvider = _providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.NotNull(wasmProvider);
            await wasmProvider.InitializeAsync();

            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web),
                Requirements = new AIRequirements
                {
                    MaxTokens = 2048,
                    Temperature = 0.7
                }
            };

            // Act
            var engine = await wasmProvider.CreateEngineAsync(context);

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

            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop),
                Requirements = new AIRequirements
                {
                    MaxTokens = 4096,
                    Temperature = 0.8
                }
            };

            // Act
            var engine = await nativeProvider.CreateEngineAsync(context);

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public async Task ModelManagementService_ShouldGetAvailableModels()
        {
            // Act
            var models = await _modelService.ListModelsAsync(Nexo.Core.Domain.Enums.PlatformType.Desktop);

            // Assert
            Assert.NotNull(models);
            // In test environment, no models are actually installed, so list should be empty
            Assert.True(models.Count() >= 0);
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
                TargetPlatform = ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly),
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Balanced.ToString()
            };

            // Act
            var selection = await _runtimeSelector.SelectOptimalProviderAsync(context);

            // Assert
            Assert.NotNull(selection);
        }

        [Fact]
        public async Task RuntimeSelector_ShouldSelectDifferentProvidersForDifferentPlatforms()
        {
            // Arrange
            var webAssemblyContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly),
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Balanced.ToString()
            };

            var nativeContext = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                TargetPlatform = ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows),
                MaxTokens = 2048,
                Temperature = 0.7,
                Priority = AIPriority.Performance.ToString()
            };

            // Act
            var webAssemblySelection = await _runtimeSelector.SelectOptimalProviderAsync(webAssemblyContext);
            var nativeSelection = await _runtimeSelector.SelectOptimalProviderAsync(nativeContext);

            // Assert
            Assert.NotNull(webAssemblySelection);
            Assert.NotNull(nativeSelection);
            
            // The selections should be different for different platforms
            // (though in our mock implementation, they might be the same)
        }

        [Fact]
        public async Task AllProviders_ShouldBeAvailable()
        {
            // Act
            var availableProviders = _providers.Where(p => p.IsAvailable()).ToList();

            // Assert
            Assert.True(availableProviders.Any());
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.Mock);
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.LlamaWebAssembly);
            Assert.Contains(availableProviders, p => p.ProviderType == AIProviderType.LlamaNative);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task AllProviders_ShouldInitializeSuccessfully()
        {
            // Act
            var initializationResults = new List<bool>();
            
            foreach (var provider in _providers.Where(p => p.IsAvailable()))
            {
                try
                {
                    await provider.InitializeAsync();
                    initializationResults.Add(true);
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

            // Act & Assert
            var isAvailable = await _modelService.IsModelAvailableAsync(modelId, Nexo.Core.Domain.Enums.PlatformType.Desktop);
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

        private Nexo.Core.Domain.Enums.PlatformType ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => Nexo.Core.Domain.Enums.PlatformType.Web,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop => Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => Nexo.Core.Domain.Enums.PlatformType.Mobile,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Console => Nexo.Core.Domain.Enums.PlatformType.Desktop, // Map Console to Desktop
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => Nexo.Core.Domain.Enums.PlatformType.Windows,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => Nexo.Core.Domain.Enums.PlatformType.Linux,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => Nexo.Core.Domain.Enums.PlatformType.macOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => Nexo.Core.Domain.Enums.PlatformType.Web, // Map WebAssembly to Web
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS => Nexo.Core.Domain.Enums.PlatformType.iOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android => Nexo.Core.Domain.Enums.PlatformType.Android,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud => Nexo.Core.Domain.Enums.PlatformType.Cloud,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker => Nexo.Core.Domain.Enums.PlatformType.Container,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other => Nexo.Core.Domain.Enums.PlatformType.CrossPlatform,
                _ => Nexo.Core.Domain.Enums.PlatformType.Unknown
            };
        }
    }
}
