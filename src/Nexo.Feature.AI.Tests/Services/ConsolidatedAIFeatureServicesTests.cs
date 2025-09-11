using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Feature.AI.Services;
using Xunit;

namespace Nexo.Feature.AI.Tests.Services
{
    /// <summary>
    /// Tests for consolidated AI feature services following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIFeatureServicesTests
    {
        private readonly Mock<ILogger<CodeGenerationService>> _mockLogger;
        private readonly Mock<IAIEngine> _mockAIEngine;

        public ConsolidatedAIFeatureServicesTests()
        {
            _mockLogger = new Mock<ILogger<CodeGenerationService>>();
            _mockAIEngine = new Mock<IAIEngine>();
        }

        [Fact]
        public void CodeGenerationService_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceId = "test-code-generation-service";
            var serviceName = "Test Code Generation Service";
            var operationType = AIOperationType.CodeGeneration;

            // Act
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                serviceId,
                serviceName,
                operationType);

            // Assert
            service.Id.Should().Be(serviceId);
            service.Name.Should().Be(serviceName);
            service.OperationType.Should().Be(operationType);
            service.IsAvailable.Should().BeFalse();
            service.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public async Task CodeGenerationService_InitializeAsync_ShouldSetInitializedAndAvailable()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            // Act
            var result = await service.InitializeAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeTrue();
            service.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task CodeGenerationService_ShutdownAsync_ShouldSetUninitializedAndUnavailable()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            await service.InitializeAsync();

            // Act
            var result = await service.ShutdownAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeFalse();
            service.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task CodeGenerationService_GetInfoAsync_ShouldReturnCorrectInfo()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            await service.InitializeAsync();

            // Act
            var info = await service.GetInfoAsync();

            // Assert
            info.Id.Should().Be("test-service");
            info.Name.Should().Be("Test Service");
            info.OperationType.Should().Be(AIOperationType.CodeGeneration);
            info.IsAvailable.Should().BeTrue();
            info.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task CodeGenerationService_IsHealthyAsync_ShouldReturnTrueWhenInitialized()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            await service.InitializeAsync();

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeTrue();
        }

        [Fact]
        public async Task CodeGenerationService_IsHealthyAsync_ShouldReturnFalseWhenNotInitialized()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeFalse();
        }

        [Fact]
        public async Task CodeGenerationService_ExecuteAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var service = new CodeGenerationService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeGeneration);

            await service.InitializeAsync();

            var request = new BaseRequest
            {
                Id = "test-request",
                Code = "Console.WriteLine(\"Hello World\");",
                Prompt = "Generate a simple hello world program",
                Language = "C#",
                Context = "Test context"
            };

            // Act
            var result = await service.ExecuteAsync<BaseResult>(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.SuccessMessage.Should().Be("Code generation completed successfully");
        }

        [Fact]
        public void CodeReviewService_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceId = "test-code-review-service";
            var serviceName = "Test Code Review Service";
            var operationType = AIOperationType.CodeReview;

            // Act
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                serviceId,
                serviceName,
                operationType);

            // Assert
            service.Id.Should().Be(serviceId);
            service.Name.Should().Be(serviceName);
            service.OperationType.Should().Be(operationType);
            service.IsAvailable.Should().BeFalse();
            service.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public async Task CodeReviewService_InitializeAsync_ShouldSetInitializedAndAvailable()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            // Act
            var result = await service.InitializeAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeTrue();
            service.IsAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task CodeReviewService_ShutdownAsync_ShouldSetUninitializedAndUnavailable()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            await service.InitializeAsync();

            // Act
            var result = await service.ShutdownAsync();

            // Assert
            result.Should().BeTrue();
            service.IsInitialized.Should().BeFalse();
            service.IsAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task CodeReviewService_GetInfoAsync_ShouldReturnCorrectInfo()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            await service.InitializeAsync();

            // Act
            var info = await service.GetInfoAsync();

            // Assert
            info.Id.Should().Be("test-service");
            info.Name.Should().Be("Test Service");
            info.OperationType.Should().Be(AIOperationType.CodeReview);
            info.IsAvailable.Should().BeTrue();
            info.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task CodeReviewService_IsHealthyAsync_ShouldReturnTrueWhenInitialized()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            await service.InitializeAsync();

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeTrue();
        }

        [Fact]
        public async Task CodeReviewService_IsHealthyAsync_ShouldReturnFalseWhenNotInitialized()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            isHealthy.Should().BeFalse();
        }

        [Fact]
        public async Task CodeReviewService_ExecuteAsync_ShouldReturnSuccessResult()
        {
            // Arrange
            var service = new CodeReviewService(
                _mockLogger.Object,
                _mockAIEngine.Object,
                "test-service",
                "Test Service",
                AIOperationType.CodeReview);

            await service.InitializeAsync();

            var request = new BaseRequest
            {
                Id = "test-request",
                Code = "Console.WriteLine(\"Hello World\");",
                Prompt = "Review this code for quality issues",
                Language = "C#",
                Context = "Test context"
            };

            // Act
            var result = await service.ExecuteAsync<BaseResult>(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.SuccessMessage.Should().Be("Code review completed successfully");
        }
    }
}
