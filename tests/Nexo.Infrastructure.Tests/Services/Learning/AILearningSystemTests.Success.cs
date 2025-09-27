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
    /// Success test cases for AI Learning System
    /// </summary>
    public partial class AILearningSystemTests
    {
        [Fact]
        public async Task LearnFromFeaturePatternAsync_ValidPattern_ReturnsSuccessResult()
        {
            // Arrange
            var featurePattern = new FeaturePattern
            {
                Id = "test-pattern-1",
                Name = "User Authentication Pattern",
                Description = "Pattern for user authentication features",
                Domain = "Security",
                Complexity = "Medium",
                Technologies = new List<string> { "C#", "ASP.NET Core", "JWT" },
                Patterns = new List<string> { "Repository", "Service", "Controller" },
                UsageCount = 15,
                SuccessRate = 0.92,
                AverageGenerationTime = TimeSpan.FromMinutes(2.5)
            };

            var mockResponse = new ModelResponse
            {
                Content = "Pattern analysis completed with 85% confidence. Key insights: Authentication pattern optimized for security and performance.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.LearnFromFeaturePatternAsync(featurePattern);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully learned from feature pattern", result.Message);
            Assert.Equal(featurePattern.Id, result.PatternId);
            Assert.True(result.Confidence > 0);
            Assert.NotEmpty(result.Insights);
            Assert.NotNull(result.Metadata);
            Assert.True(result.ProcessedAt > DateTimeOffset.MinValue);

            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.Is<string>(s => s.Contains("User Authentication Pattern")), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AccumulateDomainKnowledgeAsync_ValidKnowledge_ReturnsSuccessResult()
        {
            // Arrange
            var domainKnowledge = new DomainKnowledge
            {
                Id = "test-knowledge-1",
                Domain = "E-commerce",
                KnowledgeType = "Business Rule",
                Content = "Users must be authenticated to place orders",
                Tags = new List<string> { "authentication", "orders", "security" },
                Confidence = 0.95,
                ReferenceCount = 25
            };

            var mockResponse = new ModelResponse
            {
                Content = "Domain knowledge processed successfully. Related knowledge identified: Order validation, Payment processing.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.AccumulateDomainKnowledgeAsync(domainKnowledge);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully accumulated domain knowledge", result.Message);
            Assert.Equal(domainKnowledge.Id, result.KnowledgeId);
            Assert.True(result.Confidence > 0);
            Assert.NotEmpty(result.RelatedKnowledge);
            Assert.NotNull(result.Metadata);
            Assert.True(result.ProcessedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AnalyzeUsagePatternsAsync_ValidUsageData_ReturnsSuccessResult()
        {
            // Arrange
            var usageData = new UsageData
            {
                Id = "test-usage-1",
                UserId = "user-123",
                FeatureId = "feature-456",
                Action = "Generate Code",
                Duration = TimeSpan.FromMinutes(3),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Usage patterns analyzed. Common patterns identified: Code generation, Testing, Documentation.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.AnalyzeUsagePatternsAsync(usageData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully analyzed usage patterns", result.Message);
            Assert.NotEmpty(result.Patterns);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.Statistics);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ProcessLearningFeedbackAsync_ValidFeedback_ReturnsSuccessResult()
        {
            // Arrange
            var feedback = new LearningFeedback
            {
                Id = "test-feedback-1",
                FeatureId = "feature-789",
                UserId = "user-456",
                FeedbackType = "Improvement",
                Content = "Code generation could be faster",
                Rating = 4,
                Timestamp = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Feedback processed successfully. Actions identified: Optimize generation speed, Improve code quality.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.ProcessLearningFeedbackAsync(feedback);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully processed learning feedback", result.Message);
            Assert.Equal(feedback.Id, result.FeedbackId);
            Assert.NotEmpty(result.Actions);
            Assert.NotNull(result.Impact);
            Assert.True(result.ProcessedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetLearningInsightsAsync_ValidContext_ReturnsInsights()
        {
            // Arrange
            var context = new LearningContext
            {
                UserId = "user-789",
                Domain = "Healthcare",
                FeatureType = "Patient Management",
                RequestTime = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Learning insights generated. Key insights: Patient data patterns, Compliance requirements, Security considerations.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.GetLearningInsightsAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.NotEmpty(result.Title);
            Assert.NotEmpty(result.Description);
            Assert.Equal(context.FeatureType, result.InsightType);
            Assert.True(result.Confidence > 0);
            Assert.NotEmpty(result.Tags);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Recommendations);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task UpdateLearningModelAsync_ValidData_ReturnsSuccessResult()
        {
            // Arrange
            var learningData = new LearningData
            {
                Id = "test-data-1",
                DataType = "Feature Pattern",
                Data = new Dictionary<string, object> { { "pattern_type", "authentication" } },
                Labels = new List<string> { "security", "authentication" },
                Weight = 0.8
            };

            var mockResponse = new ModelResponse
            {
                Content = "Learning model updated successfully. New version: 1.1.0. Metrics: accuracy=0.92, precision=0.89, recall=0.91.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.UpdateLearningModelAsync(learningData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully updated learning model", result.Message);
            Assert.NotEmpty(result.ModelId);
            Assert.NotEmpty(result.Version);
            Assert.NotNull(result.Metrics);
            Assert.True(result.UpdatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ValidateLearningEffectivenessAsync_ValidData_ReturnsValidationResult()
        {
            // Arrange
            var validationData = new ValidationData
            {
                Id = "test-validation-1",
                ValidationType = "Accuracy",
                TestData = new Dictionary<string, object> { { "test_cases", 100 } },
                ExpectedResults = new Dictionary<string, object> { { "accuracy", 0.9 } }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Learning effectiveness validated. Accuracy: 0.92, Precision: 0.89, Recall: 0.91, F1: 0.90.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.ValidateLearningEffectivenessAsync(validationData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully validated learning effectiveness", result.Message);
            Assert.True(result.Accuracy > 0);
            Assert.True(result.Precision > 0);
            Assert.True(result.Recall > 0);
            Assert.True(result.F1Score > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ValidatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ExportLearningDataAsync_ValidOptions_ReturnsExportData()
        {
            // Arrange
            var exportOptions = new LearningDataExportOptions
            {
                Format = "JSON",
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
                DataTypes = new List<string> { "FeaturePatterns", "DomainKnowledge" },
                IncludeMetadata = true,
                Compress = false
            };

            var mockResponse = new ModelResponse
            {
                Content = "Learning data exported successfully. Format: JSON, Size: 1024 bytes, Records: 1000.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _aiLearningSystem.ExportLearningDataAsync(exportOptions);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.Equal(exportOptions.Format, result.Format);
            Assert.NotNull(result.Data);
            Assert.True(result.Size > 0);
            Assert.True(result.ExportedAt > DateTimeOffset.MinValue);
            Assert.NotNull(result.Metadata);
        }
    }
}
