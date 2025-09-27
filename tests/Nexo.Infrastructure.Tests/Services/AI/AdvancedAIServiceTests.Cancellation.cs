using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.AI;
using Nexo.Infrastructure.Services.AI;

namespace Nexo.Infrastructure.Tests.Services.AI
{
    /// <summary>
    /// Cancellation test cases for Advanced AI Service - timeout, cancellation, and async operation handling
    /// </summary>
    public partial class AdvancedAIServiceTests
    {
        [Fact]
        public async Task ProcessNaturalLanguageAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var nlpRequest = new NLPRequest
            {
                Id = "test-nlp-cancel",
                Text = "Cancel text"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _advancedAIService.ProcessNaturalLanguageAsync(nlpRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateIntelligentCodeAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var codeRequest = new IntelligentCodeRequest
            {
                Id = "test-code-cancel",
                Description = "Cancel description"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _advancedAIService.GenerateIntelligentCodeAsync(codeRequest, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task OptimizeCodeAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var codeData = new CodeOptimizationData
            {
                Id = "test-optimization-cancel",
                Code = "Cancel code"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _advancedAIService.OptimizeCodeAsync(codeData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task AnalyzeCodeQualityAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var codeData = new CodeQualityData
            {
                Id = "test-quality-cancel",
                Code = "Cancel code"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _advancedAIService.AnalyzeCodeQualityAsync(codeData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GenerateDocumentationAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var codeData = new CodeDocumentationData
            {
                Id = "test-doc-cancel",
                Code = "Cancel code"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _advancedAIService.GenerateDocumentationAsync(codeData, cancellationTokenSource.Token));
        }
    }
}
