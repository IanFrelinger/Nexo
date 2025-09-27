using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Learning;
using Nexo.Infrastructure.Services.Learning;

namespace Nexo.Infrastructure.Tests.Services.Learning
{
    /// <summary>
    /// Error handling tests for OptimizationRecommendationServiceTests.
    /// </summary>
    public partial class OptimizationRecommendationServiceTests
    {
        [Fact]
        public async Task AnalyzeUsagePatternsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var usageData = new UsageData
            {
                Id = "test-usage-error",
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.AnalyzeUsagePatternsAsync(usageData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(usageData.Id, result.UsageId);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateOptimizationSuggestionsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var optimizationRequest = new OptimizationRequest
            {
                Id = "test-optimization-error",
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.GenerateOptimizationSuggestionsAsync(optimizationRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(optimizationRequest.Id, result.RequestId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task RecommendPerformanceImprovementsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var performanceData = new PerformanceData
            {
                Id = "test-performance-error",
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.RecommendPerformanceImprovementsAsync(performanceData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(performanceData.Id, result.PerformanceId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AnalyzeFeatureComplexityAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var featureData = new FeatureData
            {
                Id = "test-feature-error",
                Name = "Error Feature"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.AnalyzeFeatureComplexityAsync(featureData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(featureData.Id, result.FeatureId);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateCodeOptimizationsAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var codeData = new CodeData
            {
                Id = "test-code-error",
                Language = "C#"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.GenerateCodeOptimizationsAsync(codeData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetOptimizationHistoryAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var historyRequest = new OptimizationHistoryRequest
            {
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.GetOptimizationHistoryAsync(historyRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.True(result.RetrievedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ValidateOptimizationAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var optimization = new Optimization
            {
                Id = "test-optimization-validate-error",
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _optimizationRecommendationService.ValidateOptimizationAsync(optimization);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(optimization.Id, result.OptimizationId);
            Assert.True(result.ValidatedAt > DateTimeOffset.MinValue);
        }
    }
}
