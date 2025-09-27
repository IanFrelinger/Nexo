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
    /// Success scenario tests for OptimizationRecommendationServiceTests.
    /// </summary>
    public partial class OptimizationRecommendationServiceTests
    {
        [Fact]
        public async Task AnalyzeUsagePatternsAsync_ValidData_ReturnsAnalysisResult()
        {
            // Arrange
            var usageData = new UsageData
            {
                Id = "test-usage-1",
                FeatureId = "feature-123",
                UserId = "user-456",
                Timestamp = DateTimeOffset.UtcNow,
                Action = "Feature Access",
                Duration = TimeSpan.FromMinutes(5),
                Metadata = new Dictionary<string, object> { { "browser", "Chrome" }, { "device", "Desktop" } }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Usage pattern analysis completed. Patterns: Peak usage at 2PM, 80% mobile users, High engagement on feature X. Recommendations: Optimize for mobile, Add caching.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.AnalyzeUsagePatternsAsync(usageData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully analyzed usage patterns", result.Message);
            Assert.Equal(usageData.Id, result.UsageId);
            Assert.NotEmpty(result.Patterns);
            Assert.NotEmpty(result.Insights);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.Metrics);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateOptimizationSuggestionsAsync_ValidRequest_ReturnsSuggestions()
        {
            // Arrange
            var optimizationRequest = new OptimizationRequest
            {
                Id = "test-optimization-1",
                FeatureId = "feature-123",
                OptimizationType = "Performance",
                CurrentMetrics = new Dictionary<string, object> { { "response_time", 2.5 }, { "memory_usage", 512 } },
                TargetMetrics = new Dictionary<string, object> { { "response_time", 1.0 }, { "memory_usage", 256 } },
                Constraints = new List<string> { "budget", "timeline" }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Optimization suggestions generated. Priority: High, Impact: 40% improvement, Effort: Medium. Suggestions: Implement caching, Optimize queries, Use async patterns.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.GenerateOptimizationSuggestionsAsync(optimizationRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated optimization suggestions", result.Message);
            Assert.Equal(optimizationRequest.Id, result.RequestId);
            Assert.NotEmpty(result.Suggestions);
            Assert.NotNull(result.Priority);
            Assert.True(result.Impact > 0);
            Assert.NotNull(result.Effort);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task RecommendPerformanceImprovementsAsync_ValidData_ReturnsRecommendations()
        {
            // Arrange
            var performanceData = new PerformanceData
            {
                Id = "test-performance-1",
                FeatureId = "feature-123",
                ResponseTime = TimeSpan.FromMilliseconds(2500),
                MemoryUsage = 512,
                CpuUsage = 75.5,
                Throughput = 100,
                ErrorRate = 0.02,
                Timestamp = DateTimeOffset.UtcNow
            };

            var mockResponse = new ModelResponse
            {
                Content = "Performance improvement recommendations generated. Critical issues: High response time, Memory leak detected. Recommendations: Add caching layer, Optimize database queries, Implement connection pooling.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.RecommendPerformanceImprovementsAsync(performanceData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated performance improvement recommendations", result.Message);
            Assert.Equal(performanceData.Id, result.PerformanceId);
            Assert.NotEmpty(result.CriticalIssues);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.Priority);
            Assert.NotNull(result.Impact);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AnalyzeFeatureComplexityAsync_ValidFeature_ReturnsComplexityAnalysis()
        {
            // Arrange
            var featureData = new FeatureData
            {
                Id = "test-feature-1",
                Name = "User Authentication",
                Description = "Complete user authentication system",
                CodeLines = 1500,
                Dependencies = new List<string> { "Identity", "JWT", "Database" },
                TestCoverage = 0.85,
                Complexity = 0.7
            };

            var mockResponse = new ModelResponse
            {
                Content = "Feature complexity analysis completed. Complexity score: 0.7, Risk level: Medium, Maintenance effort: High. Recommendations: Refactor into smaller modules, Increase test coverage, Reduce dependencies.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.AnalyzeFeatureComplexityAsync(featureData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully analyzed feature complexity", result.Message);
            Assert.Equal(featureData.Id, result.FeatureId);
            Assert.True(result.ComplexityScore > 0);
            Assert.NotNull(result.RiskLevel);
            Assert.NotNull(result.MaintenanceEffort);
            Assert.NotEmpty(result.Recommendations);
            Assert.NotNull(result.Metrics);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateCodeOptimizationsAsync_ValidCode_ReturnsOptimizations()
        {
            // Arrange
            var codeData = new CodeData
            {
                Id = "test-code-1",
                Language = "C#",
                Code = "public class TestClass { public void Method() { /* inefficient code */ } }",
                FilePath = "/src/TestClass.cs",
                LineCount = 100,
                Complexity = 0.6
            };

            var mockResponse = new ModelResponse
            {
                Content = "Code optimization suggestions generated. Issues: Inefficient loops, Missing null checks, Unused variables. Optimizations: Use LINQ, Add null checks, Remove unused code. Performance gain: 30%.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.GenerateCodeOptimizationsAsync(codeData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated code optimizations", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.NotEmpty(result.Issues);
            Assert.NotEmpty(result.Optimizations);
            Assert.True(result.PerformanceGain > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GetOptimizationHistoryAsync_ValidRequest_ReturnsHistory()
        {
            // Arrange
            var historyRequest = new OptimizationHistoryRequest
            {
                FeatureId = "feature-123",
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
                IncludeMetrics = true
            };

            var mockResponse = new ModelResponse
            {
                Content = "Optimization history retrieved. Total optimizations: 15, Successful: 12, Failed: 3. Average improvement: 25%, Most effective: Caching implementation.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.GetOptimizationHistoryAsync(historyRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully retrieved optimization history", result.Message);
            Assert.True(result.TotalOptimizations > 0);
            Assert.True(result.SuccessfulOptimizations > 0);
            Assert.True(result.FailedOptimizations >= 0);
            Assert.True(result.AverageImprovement > 0);
            Assert.NotEmpty(result.MostEffectiveOptimization);
            Assert.NotNull(result.Metrics);
            Assert.True(result.RetrievedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ValidateOptimizationAsync_ValidOptimization_ReturnsValidationResult()
        {
            // Arrange
            var optimization = new Optimization
            {
                Id = "test-optimization-validate",
                FeatureId = "feature-123",
                Type = "Performance",
                Description = "Add caching layer",
                ExpectedImpact = 0.3,
                Implementation = "Implement Redis caching"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Optimization validation completed. Valid: true, Confidence: 0.85, Risk level: Low, Expected impact: 30%, Implementation complexity: Medium.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _optimizationRecommendationService.ValidateOptimizationAsync(optimization);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully validated optimization", result.Message);
            Assert.Equal(optimization.Id, result.OptimizationId);
            Assert.True(result.IsValid);
            Assert.True(result.Confidence > 0);
            Assert.NotNull(result.RiskLevel);
            Assert.True(result.ExpectedImpact > 0);
            Assert.NotNull(result.ImplementationComplexity);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ValidatedAt > DateTimeOffset.MinValue);
        }
    }
}
