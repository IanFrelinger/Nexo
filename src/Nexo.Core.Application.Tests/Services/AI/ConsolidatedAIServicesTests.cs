using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Application.Services.AI;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.AI
{
    /// <summary>
    /// Tests for consolidated AI services following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIServicesTests
    {
        private readonly Mock<ILogger<AIEngineService>> _mockLogger;

        public ConsolidatedAIServicesTests()
        {
            _mockLogger = new Mock<ILogger<AIEngineService>>();
        }

        [Fact]
        public void AIEngineService_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var engineId = "test-engine";
            var engineName = "Test AI Engine";
            var engineType = AIEngineType.Llama;
            var version = "1.0.0";

            // Act
            var service = new AIEngineService(
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
        public async Task AIEngineService_InitializeAsync_ShouldSetInitializedAndAvailable()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var result = await service.InitializeAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeTrue();
            service.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task AIEngineService_ShutdownAsync_ShouldSetUninitializedAndUnavailable()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            await service.InitializeAsync();

            // Act
            var result = await service.ShutdownAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeFalse();
            service.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task AIEngineService_GetInfoAsync_ShouldReturnCorrectInfo()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            await service.InitializeAsync();

            // Act
            var info = await service.GetInfoAsync();

            // Assert
            info.Id.Should().Be("test-engine");
            info.Name.Should().Be("Test AI Engine");
            info.EngineType.Should().Be(AIEngineType.Llama);
            info.Version.Should().Be("1.0.0");
            info.IsAvailable.Should().BeTrue();
            info.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task AIEngineService_IsHealthyAsync_ShouldReturnTrueWhenInitialized()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            await service.InitializeAsync();

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeTrue();
        }

        [Fact]
        public async Task AIEngineService_IsHealthyAsync_ShouldReturnFalseWhenNotInitialized()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeFalse();
        }

        [Fact]
        public async Task AIEngineService_GetCapabilitiesAsync_ShouldReturnCapabilities()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var capabilities = await service.GetCapabilitiesAsync();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.Should().BeOfType<Dictionary<string, object>>();
        }

        [Fact]
        public async Task AIEngineService_GetSupportedLanguagesAsync_ShouldReturnLanguages()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var languages = await service.GetSupportedLanguagesAsync();

            // Assert
            languages.Should().NotBeNull();
            languages.Should().BeOfType<List<string>>();
        }

        [Fact]
        public async Task AIEngineService_GetRequirementsAsync_ShouldReturnRequirements()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var requirements = await service.GetRequirementsAsync();

            // Assert
            requirements.Should().NotBeNull();
            requirements.Should().BeOfType<AIRequirements>();
        }

        [Fact]
        public async Task AIEngineService_GetPerformanceAsync_ShouldReturnPerformance()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var performance = await service.GetPerformanceAsync();

            // Assert
            performance.Should().NotBeNull();
            performance.Should().BeOfType<PerformanceEstimate>();
        }

        [Fact]
        public async Task AIEngineService_GetEnvironmentAsync_ShouldReturnEnvironment()
        {
            // Arrange
            var service = new AIEngineService(
                _mockLogger.Object,
                "test-engine",
                "Test AI Engine",
                AIEngineType.Llama,
                "1.0.0");

            // Act
            var environment = await service.GetEnvironmentAsync();

            // Assert
            environment.Should().NotBeNull();
            environment.Should().BeOfType<EnvironmentProfile>();
        }
    }
}
