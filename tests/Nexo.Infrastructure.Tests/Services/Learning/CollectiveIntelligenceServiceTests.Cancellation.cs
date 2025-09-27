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
    /// Cancellation tests for Collective Intelligence Service
    /// </summary>
    public partial class CollectiveIntelligenceServiceTests : IDisposable
    {
        [Fact]
        public async Task ShareFeatureKnowledgeAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var featureKnowledge = new FeatureKnowledge
            {
                Id = "test-knowledge-cancel",
                FeatureId = "feature-cancel"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.ShareFeatureKnowledgeAsync(featureKnowledge, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task LearnFromProjectAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var projectData = new ProjectData
            {
                Id = "test-project-cancel",
                Name = "Cancel Project"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.LearnFromProjectAsync(projectData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task RecognizeIndustryPatternAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var industryPattern = new IndustryPattern
            {
                Id = "test-pattern-cancel",
                Name = "Cancel Pattern"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.RecognizeIndustryPatternAsync(industryPattern, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task CreateIntelligenceDatabaseAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var intelligenceData = new IntelligenceData
            {
                Id = "test-intelligence-cancel",
                DataType = "Cancel Type"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.CreateIntelligenceDatabaseAsync(intelligenceData, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task SearchIntelligenceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var searchQuery = new IntelligenceSearchQuery
            {
                Query = "cancel query"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.SearchIntelligenceAsync(searchQuery, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task GetIntelligenceStatisticsAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.GetIntelligenceStatisticsAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ExportIntelligenceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var exportOptions = new IntelligenceExportOptions
            {
                Format = "JSON"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.ExportIntelligenceAsync(exportOptions, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task ImportIntelligenceAsync_CancellationTokenCancelled_ThrowsOperationCancelledException()
        {
            // Arrange
            var importData = new IntelligenceImportData
            {
                Id = "test-import-cancel",
                Format = "JSON"
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _collectiveIntelligenceService.ImportIntelligenceAsync(importData, cancellationTokenSource.Token));
        }
    }
}
