using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Predictive;
using Nexo.Infrastructure.Services.Predictive;

namespace Nexo.Infrastructure.Tests.Services.Predictive.Cancellation
{
    /// <summary>
    /// Tests for cancellation token behavior in Predictive Development Service
    /// </summary>
    public class CancellationTests
    {
        private readonly Mock<ILogger<PredictiveDevelopmentService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly PredictiveDevelopmentService _predictiveDevelopmentService;

        public CancellationTests()
        {
            _mockLogger = new Mock<ILogger<PredictiveDevelopmentService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _predictiveDevelopmentService = new PredictiveDevelopmentService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task PredictFeatureComplexityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var featureData = new FeaturePredictionData
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
                () => _predictiveDevelopmentService.PredictFeatureComplexityAsync(featureData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task EstimateDevelopmentTimeAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var estimationRequest = new DevelopmentTimeEstimationRequest
            {
                Id = "test-estimation-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _predictiveDevelopmentService.EstimateDevelopmentTimeAsync(estimationRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AssessRiskAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var riskData = new RiskAssessmentData
            {
                Id = "test-risk-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _predictiveDevelopmentService.AssessRiskAsync(riskData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task PredictResourceRequirementsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var resourceData = new ResourcePredictionData
            {
                Id = "test-resource-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _predictiveDevelopmentService.PredictResourceRequirementsAsync(resourceData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateDevelopmentPlanAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var planRequest = new DevelopmentPlanRequest
            {
                Id = "test-plan-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _predictiveDevelopmentService.GenerateDevelopmentPlanAsync(planRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AnalyzePerformanceImpactAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var performanceData = new PerformanceImpactData
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
                () => _predictiveDevelopmentService.AnalyzePerformanceImpactAsync(performanceData, cancellationTokenSource.Token));
        }
    }
}
