using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Xunit;

namespace Nexo.Core.Domain.Tests.Entities.AI
{
    /// <summary>
    /// Tests for consolidated AI results following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIResultsTests
    {
        [Fact]
        public void CodeGenerationResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new CodeGenerationResult();

            // Assert
            result.Id.Should().NotBeNullOrEmpty();
            result.Name.Should().Be(string.Empty);
            result.Description.Should().Be(string.Empty);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty();
            result.RequestId.Should().Be(string.Empty);
            result.GeneratedCode.Should().Be(string.Empty);
            result.Explanation.Should().Be(string.Empty);
            result.Suggestions.Should().NotBeNull();
            result.Suggestions.Should().BeEmpty();
            result.Warnings.Should().NotBeNull();
            result.Warnings.Should().BeEmpty();
            result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CodeGenerationResult_ShouldInheritFromAIOperationResult()
        {
            // Arrange
            var resultType = typeof(CodeGenerationResult);

            // Assert
            resultType.BaseType.Should().Be(typeof(AIOperationResult));
        }

        [Fact]
        public void CodeGenerationResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new CodeGenerationResult();
            var testRequestId = "test-request-id";
            var testGeneratedCode = "Console.WriteLine(\"Hello World\");";
            var testExplanation = "This is a simple hello world program";
            var testSuggestions = new List<string> { "Add error handling", "Add comments" };
            var testWarnings = new List<string> { "Consider using async/await" };
            var testGeneratedAt = DateTime.UtcNow.AddMinutes(-5);

            // Act
            result.RequestId = testRequestId;
            result.GeneratedCode = testGeneratedCode;
            result.Explanation = testExplanation;
            result.Suggestions = testSuggestions;
            result.Warnings = testWarnings;
            result.GeneratedAt = testGeneratedAt;

            // Assert
            result.RequestId.Should().Be(testRequestId);
            result.GeneratedCode.Should().Be(testGeneratedCode);
            result.Explanation.Should().Be(testExplanation);
            result.Suggestions.Should().Be(testSuggestions);
            result.Warnings.Should().Be(testWarnings);
            result.GeneratedAt.Should().Be(testGeneratedAt);
        }

        [Fact]
        public void CodeReviewResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new CodeReviewResult();

            // Assert
            result.Id.Should().NotBeNullOrEmpty();
            result.Name.Should().Be(string.Empty);
            result.Description.Should().Be(string.Empty);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty();
            result.Code.Should().Be(string.Empty);
            result.Issues.Should().NotBeNull();
            result.Issues.Should().BeEmpty();
            result.Suggestions.Should().NotBeNull();
            result.Suggestions.Should().BeEmpty();
            result.QualityScore.Should().Be(0);
            result.ReviewTime.Should().Be(TimeSpan.Zero);
            result.ReviewedCode.Should().Be(string.Empty);
            result.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CodeReviewResult_ShouldInheritFromAIOperationResult()
        {
            // Arrange
            var resultType = typeof(CodeReviewResult);

            // Assert
            resultType.BaseType.Should().Be(typeof(AIOperationResult));
        }

        [Fact]
        public void CodeReviewResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new CodeReviewResult();
            var testCode = "Console.WriteLine(\"Hello World\");";
            var testIssues = new List<CodeIssue>
            {
                new CodeIssue { Type = "Error", Message = "Missing semicolon" },
                new CodeIssue { Type = "Warning", Message = "Consider using async/await" }
            };
            var testSuggestions = new List<CodeSuggestion>
            {
                new CodeSuggestion { Type = "Improvement", Description = "Add error handling" }
            };
            var testQualityScore = 8.5;
            var testReviewTime = TimeSpan.FromMinutes(2);
            var testReviewedCode = "Console.WriteLine(\"Hello World\"); // Fixed";
            var testReviewedAt = DateTime.UtcNow.AddMinutes(-3);

            // Act
            result.Code = testCode;
            result.Issues = testIssues;
            result.Suggestions = testSuggestions;
            result.QualityScore = testQualityScore;
            result.ReviewTime = testReviewTime;
            result.ReviewedCode = testReviewedCode;
            result.ReviewedAt = testReviewedAt;

            // Assert
            result.Code.Should().Be(testCode);
            result.Issues.Should().Be(testIssues);
            result.Suggestions.Should().Be(testSuggestions);
            result.QualityScore.Should().Be(testQualityScore);
            result.ReviewTime.Should().Be(testReviewTime);
            result.ReviewedCode.Should().Be(testReviewedCode);
            result.ReviewedAt.Should().Be(testReviewedAt);
        }

        [Fact]
        public void CodeOptimizationResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new CodeOptimizationResult();

            // Assert
            result.Id.Should().NotBeNullOrEmpty();
            result.Name.Should().Be(string.Empty);
            result.Description.Should().Be(string.Empty);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty();
            result.OptimizedCode.Should().Be(string.Empty);
            result.OptimizationScore.Should().Be(0);
            result.Improvements.Should().NotBeNull();
            result.Improvements.Should().BeEmpty();
            result.PerformanceGain.Should().Be(0);
            result.OptimizationTime.Should().Be(TimeSpan.Zero);
            result.OriginalCode.Should().Be(string.Empty);
            result.OptimizedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CodeOptimizationResult_ShouldInheritFromAIOperationResult()
        {
            // Arrange
            var resultType = typeof(CodeOptimizationResult);

            // Assert
            resultType.BaseType.Should().Be(typeof(AIOperationResult));
        }

        [Fact]
        public void CodeOptimizationResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new CodeOptimizationResult();
            var testOptimizedCode = "Console.WriteLine(\"Hello World\"); // Optimized";
            var testOptimizationScore = 9.2;
            var testImprovements = new List<string> { "Reduced memory allocation", "Improved performance" };
            var testPerformanceGain = 0.25;
            var testOptimizationTime = TimeSpan.FromMinutes(1);
            var testOriginalCode = "Console.WriteLine(\"Hello World\");";
            var testOptimizedAt = DateTime.UtcNow.AddMinutes(-2);

            // Act
            result.OptimizedCode = testOptimizedCode;
            result.OptimizationScore = testOptimizationScore;
            result.Improvements = testImprovements;
            result.PerformanceGain = testPerformanceGain;
            result.OptimizationTime = testOptimizationTime;
            result.OriginalCode = testOriginalCode;
            result.OptimizedAt = testOptimizedAt;

            // Assert
            result.OptimizedCode.Should().Be(testOptimizedCode);
            result.OptimizationScore.Should().Be(testOptimizationScore);
            result.Improvements.Should().Be(testImprovements);
            result.PerformanceGain.Should().Be(testPerformanceGain);
            result.OptimizationTime.Should().Be(testOptimizationTime);
            result.OriginalCode.Should().Be(testOriginalCode);
            result.OptimizedAt.Should().Be(testOptimizedAt);
        }

        [Fact]
        public void DocumentationResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new DocumentationResult();

            // Assert
            result.Id.Should().NotBeNullOrEmpty();
            result.Name.Should().Be(string.Empty);
            result.Description.Should().Be(string.Empty);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty();
            result.GeneratedDocumentation.Should().Be(string.Empty);
            result.DocumentationType.Should().Be(DocumentationType.API);
            result.QualityScore.Should().Be(0);
            result.Coverage.Should().Be(0);
            result.GenerationTime.Should().Be(DateTime.MinValue);
            result.Tags.Should().NotBeNull();
            result.Tags.Should().BeEmpty();
        }

        [Fact]
        public void DocumentationResult_ShouldInheritFromAIOperationResult()
        {
            // Arrange
            var resultType = typeof(DocumentationResult);

            // Assert
            resultType.BaseType.Should().Be(typeof(AIOperationResult));
        }

        [Fact]
        public void DocumentationResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new DocumentationResult();
            var testGeneratedDocumentation = "This is the generated documentation";
            var testDocumentationType = DocumentationType.User;
            var testQualityScore = 8;
            var testCoverage = 90;
            var testGenerationTime = DateTime.UtcNow.AddMinutes(-1);
            var testTags = new List<string> { "API", "User Guide", "Examples" };

            // Act
            result.GeneratedDocumentation = testGeneratedDocumentation;
            result.DocumentationType = testDocumentationType;
            result.QualityScore = testQualityScore;
            result.Coverage = testCoverage;
            result.GenerationTime = testGenerationTime;
            result.Tags = testTags;

            // Assert
            result.GeneratedDocumentation.Should().Be(testGeneratedDocumentation);
            result.DocumentationType.Should().Be(testDocumentationType);
            result.QualityScore.Should().Be(testQualityScore);
            result.Coverage.Should().Be(testCoverage);
            result.GenerationTime.Should().Be(testGenerationTime);
            result.Tags.Should().Be(testTags);
        }

        [Fact]
        public void TestingResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new TestingResult();

            // Assert
            result.Id.Should().NotBeNullOrEmpty();
            result.Name.Should().Be(string.Empty);
            result.Description.Should().Be(string.Empty);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.Metadata.Should().NotBeNull();
            result.Metadata.Should().BeEmpty();
            result.GeneratedTests.Should().Be(string.Empty);
            result.QualityScore.Should().Be(0);
            result.Coverage.Should().Be(0);
            result.TestResults.Should().NotBeNull();
            result.TestResults.Should().BeEmpty();
            result.Suggestions.Should().NotBeNull();
            result.Suggestions.Should().BeEmpty();
            result.SuccessMessage.Should().BeNull();
            result.Duration.Should().Be(TimeSpan.Zero);
            result.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void TestingResult_ShouldInheritFromAIOperationResult()
        {
            // Arrange
            var resultType = typeof(TestingResult);

            // Assert
            resultType.BaseType.Should().Be(typeof(AIOperationResult));
        }

        [Fact]
        public void TestingResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new TestingResult();
            var testGeneratedTests = "This is the generated test code";
            var testQualityScore = 9;
            var testCoverage = 95;
            var testTestResults = new List<TestResult>
            {
                new TestResult { Name = "Test1", Passed = true },
                new TestResult { Name = "Test2", Passed = false }
            };
            var testSuggestions = new List<string> { "Add more edge cases", "Improve test coverage" };
            var testSuccessMessage = "Tests generated successfully";
            var testDuration = TimeSpan.FromMinutes(3);
            var testCompletedAt = DateTime.UtcNow.AddMinutes(-4);

            // Act
            result.GeneratedTests = testGeneratedTests;
            result.QualityScore = testQualityScore;
            result.Coverage = testCoverage;
            result.TestResults = testTestResults;
            result.Suggestions = testSuggestions;
            result.SuccessMessage = testSuccessMessage;
            result.Duration = testDuration;
            result.CompletedAt = testCompletedAt;

            // Assert
            result.GeneratedTests.Should().Be(testGeneratedTests);
            result.QualityScore.Should().Be(testQualityScore);
            result.Coverage.Should().Be(testCoverage);
            result.TestResults.Should().Be(testTestResults);
            result.Suggestions.Should().Be(testSuggestions);
            result.SuccessMessage.Should().Be(testSuccessMessage);
            result.Duration.Should().Be(testDuration);
            result.CompletedAt.Should().Be(testCompletedAt);
        }
    }
}