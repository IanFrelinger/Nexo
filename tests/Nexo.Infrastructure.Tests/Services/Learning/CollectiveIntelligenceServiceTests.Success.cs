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
    /// Success scenario tests for Collective Intelligence Service
    /// </summary>
    public partial class CollectiveIntelligenceServiceTests : IDisposable
    {
        [Fact]
        public async Task ShareFeatureKnowledgeAsync_ValidKnowledge_ReturnsSuccessResult()
        {
            // Arrange
            var featureKnowledge = new FeatureKnowledge
            {
                Id = "test-knowledge-1",
                FeatureId = "feature-123",
                ProjectId = "project-456",
                KnowledgeType = "Best Practice",
                Content = "Use dependency injection for better testability",
                Tags = new List<string> { "dependency-injection", "testing", "architecture" },
                Confidence = 0.95,
                CreatedBy = "developer-1",
                ShareCount = 5,
                Rating = 4.5
            };

            var mockResponse = new ModelResponse
            {
                Content = "Feature knowledge shared successfully. Recipients: Project A, Project B, Project C. Share count: 5.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.ShareFeatureKnowledgeAsync(featureKnowledge);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully shared feature knowledge", result.Message);
            Assert.Equal(featureKnowledge.Id, result.KnowledgeId);
            Assert.True(result.ShareCount > 0);
            Assert.NotEmpty(result.Recipients);
            Assert.NotNull(result.Metrics);
            Assert.True(result.SharedAt > DateTimeOffset.MinValue);

            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.Is<string>(s => s.Contains("Best Practice")), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LearnFromProjectAsync_ValidProjectData_ReturnsSuccessResult()
        {
            // Arrange
            var projectData = new ProjectData
            {
                Id = "test-project-1",
                Name = "E-commerce Platform",
                Description = "Modern e-commerce platform with microservices architecture",
                Domain = "E-commerce",
                Technology = "C# .NET Core",
                Features = new List<string> { "User Management", "Product Catalog", "Order Processing" },
                Patterns = new List<string> { "Microservices", "CQRS", "Event Sourcing" },
                Metrics = new Dictionary<string, object> { { "performance_score", 0.92 }, { "maintainability", 0.88 } }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Project learning completed. Learned patterns: Microservices, CQRS. Insights: High performance, Good maintainability.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.LearnFromProjectAsync(projectData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully learned from project", result.Message);
            Assert.Equal(projectData.Id, result.ProjectId);
            Assert.NotEmpty(result.LearnedPatterns);
            Assert.NotEmpty(result.Insights);
            Assert.NotNull(result.Metrics);
            Assert.True(result.LearnedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task RecognizeIndustryPatternAsync_ValidPattern_ReturnsSuccessResult()
        {
            // Arrange
            var industryPattern = new IndustryPattern
            {
                Id = "test-pattern-1",
                Name = "Microservices Architecture",
                Description = "Distributed system architecture pattern",
                Industry = "Software Development",
                Category = "Architecture",
                Technologies = new List<string> { "Docker", "Kubernetes", "API Gateway" },
                Examples = new List<string> { "Netflix", "Amazon", "Uber" },
                Frequency = 0.85,
                Confidence = 0.92
            };

            var mockResponse = new ModelResponse
            {
                Content = "Industry pattern recognized successfully. Matches: Netflix, Amazon. Confidence: 0.92. Recommendations: Use API Gateway, Implement service discovery.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.RecognizeIndustryPatternAsync(industryPattern);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully recognized industry pattern", result.Message);
            Assert.Equal(industryPattern.Id, result.PatternId);
            Assert.True(result.Confidence > 0);
            Assert.NotEmpty(result.Matches);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.Metadata);
            Assert.True(result.RecognizedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task CreateIntelligenceDatabaseAsync_ValidData_ReturnsSuccessResult()
        {
            // Arrange
            var intelligenceData = new IntelligenceData
            {
                Id = "test-intelligence-1",
                DataType = "Feature Patterns",
                Data = new Dictionary<string, object> { { "pattern_count", 100 }, { "success_rate", 0.95 } },
                Categories = new List<string> { "Authentication", "Authorization", "Data Access" },
                Weight = 0.8
            };

            var mockResponse = new ModelResponse
            {
                Content = "Intelligence database created successfully. Record count: 1000. Schema: 5 tables, 12 indexes.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.CreateIntelligenceDatabaseAsync(intelligenceData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully created intelligence database", result.Message);
            Assert.NotEmpty(result.DatabaseId);
            Assert.True(result.RecordCount > 0);
            Assert.NotNull(result.Schema);
            Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task SearchIntelligenceAsync_ValidQuery_ReturnsSearchResults()
        {
            // Arrange
            var searchQuery = new IntelligenceSearchQuery
            {
                Query = "microservices authentication",
                Categories = new List<string> { "Architecture", "Security" },
                Tags = new List<string> { "microservices", "auth" },
                MaxResults = 50,
                SortBy = "relevance"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Intelligence search completed. Found 25 items. Total count: 100. Page count: 4. Facets: categories, tags.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.SearchIntelligenceAsync(searchQuery);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully searched collective intelligence", result.Message);
            Assert.NotEmpty(result.Items);
            Assert.True(result.TotalCount > 0);
            Assert.True(result.PageCount > 0);
            Assert.NotNull(result.Facets);
            Assert.True(result.SearchedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetIntelligenceStatisticsAsync_ValidRequest_ReturnsStatistics()
        {
            // Arrange
            var mockResponse = new ModelResponse
            {
                Content = "Intelligence statistics generated. Total items: 10000, Projects: 500, Patterns: 2000, Knowledge: 5000. Quality metrics: accuracy=0.92, completeness=0.88.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.GetIntelligenceStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalItems > 0);
            Assert.True(result.TotalProjects > 0);
            Assert.True(result.TotalPatterns > 0);
            Assert.True(result.TotalKnowledge > 0);
            Assert.NotNull(result.CategoryCounts);
            Assert.NotNull(result.QualityMetrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ExportIntelligenceAsync_ValidOptions_ReturnsExportData()
        {
            // Arrange
            var exportOptions = new IntelligenceExportOptions
            {
                Format = "JSON",
                DataTypes = new List<string> { "FeaturePatterns", "IndustryPatterns" },
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
                IncludeMetadata = true,
                Compress = false
            };

            var mockResponse = new ModelResponse
            {
                Content = "Intelligence export completed. Format: JSON, Size: 2048 bytes, Item count: 1000. Metadata: export_format=JSON, compression=none.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.ExportIntelligenceAsync(exportOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.Equal(exportOptions.Format, result.Format);
            Assert.NotNull(result.Data);
            Assert.True(result.Size > 0);
            Assert.True(result.ItemCount > 0);
            Assert.True(result.ExportedAt > DateTimeOffset.MinValue);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task ImportIntelligenceAsync_ValidData_ReturnsImportResult()
        {
            // Arrange
            var importData = new IntelligenceImportData
            {
                Id = "test-import-1",
                Format = "JSON",
                Data = System.Text.Encoding.UTF8.GetBytes("{\"test\": \"data\"}"),
                Source = "External System"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Intelligence import completed. Imported: 950, Skipped: 30, Errors: 20. Import rate: 0.95, Error rate: 0.02.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _collectiveIntelligenceService.ImportIntelligenceAsync(importData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully imported collective intelligence data", result.Message);
            Assert.True(result.ImportedCount > 0);
            Assert.True(result.SkippedCount >= 0);
            Assert.True(result.ErrorCount >= 0);
            Assert.NotNull(result.Errors);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ImportedAt > DateTimeOffset.MinValue);
        }
    }
}
