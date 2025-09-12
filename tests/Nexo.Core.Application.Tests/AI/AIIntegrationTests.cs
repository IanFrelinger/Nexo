using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums;
using Xunit;

namespace Nexo.Core.Application.Tests.AI
{
    /// <summary>
    /// Tests for AI integration functionality
    /// </summary>
    public class AIIntegrationTests
    {
        private readonly ServiceProvider _serviceProvider;

        public AIIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAIServices();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task AIRuntimeSelector_ShouldSelectMockProvider()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements
                {
                    Priority = AIPriority.Balanced,
                    RequiresOffline = true
                },
                Resources = new AIResources
                {
                    AvailableMemory = 1024 * 1024 * 1024, // 1GB
                    CpuCores = 4
                }
            };

            // Act
            var engine = await selector.SelectBestEngineAsync(context);

            // Assert
            Assert.NotNull(engine);
            Assert.Equal(AIProviderType.Mock, engine.EngineInfo.ProviderType);
        }

        [Fact]
        public async Task MockAIEngine_ShouldGenerateCode()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var request = new CodeGenerationRequest
            {
                Prompt = "Create a simple calculator class in C#",
                Language = "csharp",
                Framework = "console"
            };

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var result = await engine.GenerateCodeAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.GeneratedCode);
            Assert.Contains("class", result.GeneratedCode);
            Assert.Contains("calculator", result.GeneratedCode.ToLower());
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task MockAIEngine_ShouldReviewCode()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeReview,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var code = @"
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var result = await engine.ReviewCodeAsync(code, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.QualityScore > 0);
            Assert.NotNull(result.Issues);
            Assert.NotNull(result.Suggestions);
        }

        [Fact]
        public async Task MockAIEngine_ShouldOptimizeCode()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeOptimization,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var code = "public int Add(int a, int b) { return a + b; }";

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var result = await engine.OptimizeCodeAsync(code, context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.GeneratedCode);
            Assert.True(result.ConfidenceScore > 0);
            Assert.Contains("Mock optimization", result.Explanation);
        }

        [Fact]
        public async Task MockAIEngine_ShouldGenerateDocumentation()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.Documentation,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var code = "public int Add(int a, int b) { return a + b; }";

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var result = await engine.GenerateDocumentationAsync(code, context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("summary", result.ToLower());
        }

        [Fact]
        public async Task MockAIEngine_ShouldGenerateTests()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.Testing,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var code = "public int Add(int a, int b) { return a + b; }";

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var result = await engine.GenerateTestsAsync(code, context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.GeneratedCode);
            Assert.Contains("Test", result.GeneratedCode);
            Assert.Contains("Assert", result.GeneratedCode);
        }

        [Fact]
        public async Task MockAIEngine_ShouldStreamResponse()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            var prompt = "Explain what a class is in programming";

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            
            var responseParts = new List<string>();
            await foreach (var part in engine.StreamResponseAsync(prompt, context))
            {
                responseParts.Add(part);
            }

            // Assert
            Assert.NotEmpty(responseParts);
            Assert.True(responseParts.Count > 1); // Should be streamed in parts
        }

        [Fact]
        public async Task AIRuntimeSelector_ShouldGetAvailableProviders()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();

            // Act
            var providers = await selector.GetAvailableProvidersAsync();

            // Assert
            Assert.NotEmpty(providers);
            Assert.Contains(providers, p => p.ProviderType == AIProviderType.Mock);
        }

        [Fact]
        public async Task AIRuntimeSelector_ShouldValidateOperation()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            // Act
            var providers = await selector.GetAvailableProvidersAsync();

            // Assert
            Assert.True(providers.Any());
        }

        [Fact]
        public async Task MockAIEngine_ShouldBeHealthy()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var isHealthy = engine.IsHealthy();

            // Assert
            Assert.True(isHealthy);
        }

        [Fact]
        public async Task MockAIEngine_ShouldReportResourceUsage()
        {
            // Arrange
            var selector = _serviceProvider.GetRequiredService<IAIRuntimeSelector>();
            var context = new AIOperationContext
            {
                OperationType = AIOperationType.CodeGeneration,
                Platform = Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Requirements = new AIRequirements(),
                Resources = new AIResources()
            };

            // Act
            var engine = await selector.SelectBestEngineAsync(context);
            await engine.InitializeAsync(new ModelInfo(), context);
            var memoryUsage = engine.GetMemoryUsage();
            var cpuUsage = engine.GetCpuUsage();

            // Assert
            Assert.True(memoryUsage > 0);
            Assert.True(cpuUsage >= 0);
        }

        private void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
