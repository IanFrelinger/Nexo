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
    /// Cancellation tests for OptimizationRecommendationServiceTests.
    /// </summary>
    public partial class OptimizationRecommendationServiceTests
    {
        [Fact]
        public async Task AnalyzeUsagePatternsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var usageData = new UsageData
            {
                Id = "test-usage-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.AnalyzeUsagePatternsAsync(usageData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateOptimizationSuggestionsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var optimizationRequest = new OptimizationRequest
            {
                Id = "test-optimization-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.GenerateOptimizationSuggestionsAsync(optimizationRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task RecommendPerformanceImprovementsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var performanceData = new PerformanceData
            {
                Id = "test-performance-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.RecommendPerformanceImprovementsAsync(performanceData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AnalyzeFeatureComplexityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var featureData = new FeatureData
            {
                Id = "test-feature-cancel",
                Name = "Cancel Feature"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.AnalyzeFeatureComplexityAsync(featureData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateCodeOptimizationsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var codeData = new CodeData
            {
                Id = "test-code-cancel",
                Language = "C#"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.GenerateCodeOptimizationsAsync(codeData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetOptimizationHistoryAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var historyRequest = new OptimizationHistoryRequest
            {
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.GetOptimizationHistoryAsync(historyRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ValidateOptimizationAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var optimization = new Optimization
            {
                Id = "test-optimization-validate-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _optimizationRecommendationService.ValidateOptimizationAsync(optimization, cancellationTokenSource.Token));
        }
    }
}
