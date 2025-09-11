using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Infrastructure.Services.AI;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.AI
{
    /// <summary>
    /// Tests for consolidated AI infrastructure services following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIInfrastructureServicesTests
    {
        private readonly Mock<ILogger<LlamaAIEngine>> _mockLogger;

        public ConsolidatedAIInfrastructureServicesTests()
        {
            _mockLogger = new Mock<ILogger<LlamaAIEngine>>();
        }

        [Fact]
        public void LlamaAIEngine_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var engineId = "test-llama-engine";
            var engineName = "Test Llama AI Engine";
            var engineType = AIEngineType.Llama;
            var version = "2.0.0";

            // Act
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                engineId,
                engineName,
                engineType,
                version);

            // Assert
            service.Id.Should().Be(engineId);
            service.Name.Should().Be(engineName);
            service.EngineType.Should().Be(engineType);
            service.Version.Should().Be(version);
            service.IsAvailable.Should().BeFalse();
            service.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public async Task LlamaAIEngine_InitializeAsync_ShouldSetInitializedAndAvailable()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var result = await service.InitializeAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeTrue();
            service.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task LlamaAIEngine_ShutdownAsync_ShouldSetUninitializedAndUnavailable()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            await service.InitializeAsync();

            // Act
            var result = await service.ShutdownAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeFalse();
            service.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task LlamaAIEngine_GetInfoAsync_ShouldReturnCorrectInfo()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            await service.InitializeAsync();

            // Act
            var info = await service.GetInfoAsync();

            // Assert
            info.Id.Should().Be("test-llama-engine");
            info.Name.Should().Be("Test Llama AI Engine");
            info.EngineType.Should().Be(AIEngineType.Llama);
            info.Version.Should().Be("2.0.0");
            info.IsAvailable.Should().BeTrue();
            info.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task LlamaAIEngine_IsHealthyAsync_ShouldReturnTrueWhenInitialized()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            await service.InitializeAsync();

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeTrue();
        }

        [Fact]
        public async Task LlamaAIEngine_IsHealthyAsync_ShouldReturnFalseWhenNotInitialized()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeFalse();
        }

        [Fact]
        public async Task LlamaAIEngine_GetCapabilitiesAsync_ShouldReturnCapabilities()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var capabilities = await service.GetCapabilitiesAsync();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.Should().BeOfType<Dictionary<string, object>>();
            capabilities.Should().ContainKey("code_generation");
            capabilities.Should().ContainKey("code_review");
            capabilities.Should().ContainKey("code_optimization");
            capabilities.Should().ContainKey("documentation");
            capabilities.Should().ContainKey("testing");
        }

        [Fact]
        public async Task LlamaAIEngine_GetSupportedLanguagesAsync_ShouldReturnLanguages()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var languages = await service.GetSupportedLanguagesAsync();

            // Assert
            languages.Should().NotBeNull();
            languages.Should().BeOfType<List<string>>();
            languages.Should().Contain("C#");
            languages.Should().Contain("Python");
            languages.Should().Contain("JavaScript");
            languages.Should().Contain("TypeScript");
            languages.Should().Contain("Java");
            languages.Should().Contain("C++");
        }

        [Fact]
        public async Task LlamaAIEngine_GetRequirementsAsync_ShouldReturnRequirements()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var requirements = await service.GetRequirementsAsync();

            // Assert
            requirements.Should().NotBeNull();
            requirements.Should().BeOfType<AIRequirements>();
            requirements.Language.Should().Be("C#");
            requirements.Framework.Should().Be(".NET");
            requirements.Platform.Should().Be("Local");
            requirements.SafetyLevel.Should().Be(SafetyLevel.Standard);
        }

        [Fact]
        public async Task LlamaAIEngine_GetPerformanceAsync_ShouldReturnPerformance()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var performance = await service.GetPerformanceAsync();

            // Assert
            performance.Should().NotBeNull();
            performance.Should().BeOfType<PerformanceEstimate>();
            performance.EstimatedTime.Should().Be(TimeSpan.FromSeconds(5));
            performance.CpuUtilization.Should().Be(0.8);
            performance.MemoryUsage.Should().Be(1024);
            performance.Confidence.Should().Be(0.9);
        }

        [Fact]
        public async Task LlamaAIEngine_GetEnvironmentAsync_ShouldReturnEnvironment()
        {
            // Arrange
            var service = new LlamaAIEngine(
                _mockLogger.Object,
                "test-llama-engine",
                "Test Llama AI Engine",
                AIEngineType.Llama,
                "2.0.0");

            // Act
            var environment = await service.GetEnvironmentAsync();

            // Assert
            environment.Should().NotBeNull();
            environment.Should().BeOfType<EnvironmentProfile>();
            environment.CurrentPlatform.Should().Be("Local");
            environment.AvailableMemory.Should().Be(8192);
            environment.PlatformType.Should().Be(PlatformType.Windows);
        }
    }
}
