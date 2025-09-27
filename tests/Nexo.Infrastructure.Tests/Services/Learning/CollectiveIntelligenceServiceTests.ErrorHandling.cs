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
    /// Error handling tests for Collective Intelligence Service
    /// </summary>
    public partial class CollectiveIntelligenceServiceTests : IDisposable
    {
        [Fact]
        public async Task ShareFeatureKnowledgeAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var featureKnowledge = new FeatureKnowledge
            {
                Id = "test-knowledge-error",
                FeatureId = "feature-error"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.ShareFeatureKnowledgeAsync(featureKnowledge);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(featureKnowledge.Id, result.KnowledgeId);
            Assert.True(result.SharedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task LearnFromProjectAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var projectData = new ProjectData
            {
                Id = "test-project-error",
                Name = "Error Project"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.LearnFromProjectAsync(projectData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(projectData.Id, result.ProjectId);
            Assert.True(result.LearnedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task RecognizeIndustryPatternAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var industryPattern = new IndustryPattern
            {
                Id = "test-pattern-error",
                Name = "Error Pattern"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.RecognizeIndustryPatternAsync(industryPattern);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(industryPattern.Id, result.PatternId);
            Assert.True(result.RecognizedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateIntelligenceDatabaseAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var intelligenceData = new IntelligenceData
            {
                Id = "test-intelligence-error",
                DataType = "Error Type"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.CreateIntelligenceDatabaseAsync(intelligenceData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.NotEmpty(result.DatabaseId);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task SearchIntelligenceAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var searchQuery = new IntelligenceSearchQuery
            {
                Query = "error query"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.SearchIntelligenceAsync(searchQuery);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.True(result.SearchedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetIntelligenceStatisticsAsync_ModelOrchestratorThrows_ReturnsEmptyStatistics()
        {
            // Arrange
            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.GetIntelligenceStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ExportIntelligenceAsync_ModelOrchestratorThrows_ReturnsEmptyExport()
        {
            // Arrange
            var exportOptions = new IntelligenceExportOptions
            {
                Format = "JSON"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.ExportIntelligenceAsync(exportOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.Equal(exportOptions.Format, result.Format);
            Assert.True(result.ExportedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ImportIntelligenceAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var importData = new IntelligenceImportData
            {
                Id = "test-import-error",
                Format = "JSON"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _collectiveIntelligenceService.ImportIntelligenceAsync(importData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.True(result.ImportedAt > DateTimeOffset.MinValue);
        }
    }
}
