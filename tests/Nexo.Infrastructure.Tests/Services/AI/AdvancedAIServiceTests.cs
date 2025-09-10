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
    /// Comprehensive E2E tests for Advanced AI Service in Phase 9.
    /// Tests all advanced AI capabilities including enhanced NLP,
    /// intelligent code generation, and code optimization.
    /// </summary>
    public class AdvancedAIServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AdvancedAIService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly AdvancedAIService _advancedAIService;

        public AdvancedAIServiceTests()
        {
            _mockLogger = new Mock<ILogger<AdvancedAIService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
            _advancedAIService = new AdvancedAIService(_mockLogger.Object, _mockModelOrchestrator.Object);
        }

        [Fact]
        public async Task ProcessNaturalLanguageAsync_ValidText_ReturnsProcessedResult()
        {
            // Arrange
            var nlpRequest = new NLPRequest
            {
                Id = "test-nlp-1",
                Text = "Create a user authentication system with JWT tokens and role-based access control",
                Language = "en",
                ProcessingType = "Code Generation",
                Context = "Web application development",
                Options = new Dictionary<string, object> { { "include_comments", true }, { "add_tests", true } }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Natural language processed successfully. Intent: Code Generation, Entities: user authentication, JWT tokens, role-based access control, Confidence: 0.95, Language: en.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _advancedAIService.ProcessNaturalLanguageAsync(nlpRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully processed natural language", result.Message);
            Assert.Equal(nlpRequest.Id, result.RequestId);
            Assert.Equal(nlpRequest.Text, result.OriginalText);
            Assert.Equal(nlpRequest.Language, result.Language);
            Assert.NotNull(result.Intent);
            Assert.NotEmpty(result.Entities);
            Assert.True(result.Confidence > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.ProcessedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateIntelligentCodeAsync_ValidCodeRequest_ReturnsGeneratedCode()
        {
            // Arrange
            var codeRequest = new IntelligentCodeRequest
            {
                Id = "test-code-1",
                Description = "Create a REST API controller for user management with CRUD operations",
                Language = "C#",
                Framework = "ASP.NET Core",
                Patterns = new List<string> { "Repository Pattern", "Dependency Injection" },
                Requirements = new List<string> { "Authentication", "Validation", "Error Handling" },
                Style = "Clean Architecture"
            };

            var mockResponse = new ModelResponse
            {
                Content = "Intelligent code generated successfully. Language: C#, Framework: ASP.NET Core, Patterns: Repository Pattern, Dependency Injection, Lines: 150, Functions: 8, Classes: 3.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _advancedAIService.GenerateIntelligentCodeAsync(codeRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated intelligent code", result.Message);
            Assert.Equal(codeRequest.Id, result.RequestId);
            Assert.Equal(codeRequest.Language, result.Language);
            Assert.Equal(codeRequest.Framework, result.Framework);
            Assert.NotEmpty(result.Patterns);
            Assert.True(result.LineCount > 0);
            Assert.True(result.FunctionCount > 0);
            Assert.True(result.ClassCount > 0);
            Assert.NotNull(result.Code);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task OptimizeCodeAsync_ValidCodeData_ReturnsOptimizedCode()
        {
            // Arrange
            var codeData = new CodeOptimizationData
            {
                Id = "test-optimization-1",
                Code = "public class TestClass { public void Method() { /* inefficient code */ } }",
                Language = "C#",
                OptimizationType = "Performance",
                TargetMetrics = new Dictionary<string, object> { { "execution_time", 0.5 }, { "memory_usage", 100 } },
                Constraints = new List<string> { "maintainability", "readability" }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Code optimization completed successfully. Type: Performance, Improvements: 3, Performance gain: 40%, Memory reduction: 25%, Lines optimized: 15.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _advancedAIService.OptimizeCodeAsync(codeData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully optimized code", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.Equal(codeData.Language, result.Language);
            Assert.Equal(codeData.OptimizationType, result.OptimizationType);
            Assert.True(result.Improvements > 0);
            Assert.True(result.PerformanceGain > 0);
            Assert.True(result.MemoryReduction > 0);
            Assert.True(result.LinesOptimized > 0);
            Assert.NotNull(result.OptimizedCode);
            Assert.NotNull(result.Metrics);
            Assert.True(result.OptimizedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AnalyzeCodeQualityAsync_ValidCodeData_ReturnsQualityAnalysis()
        {
            // Arrange
            var codeData = new CodeQualityData
            {
                Id = "test-quality-1",
                Code = "public class TestClass { public void Method() { } }",
                Language = "C#",
                QualityMetrics = new List<string> { "complexity", "maintainability", "testability" },
                Standards = new List<string> { "SOLID", "Clean Code", "DRY" }
            };

            var mockResponse = new ModelResponse
            {
                Content = "Code quality analysis completed. Complexity: 0.3, Maintainability: 0.85, Testability: 0.9, Overall score: 0.8, Issues: 2, Recommendations: 5.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _advancedAIService.AnalyzeCodeQualityAsync(codeData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully analyzed code quality", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.Equal(codeData.Language, result.Language);
            Assert.True(result.Complexity > 0);
            Assert.True(result.Maintainability > 0);
            Assert.True(result.Testability > 0);
            Assert.True(result.OverallScore > 0);
            Assert.True(result.Issues >= 0);
            Assert.True(result.Recommendations > 0);
            Assert.NotNull(result.Metrics);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateDocumentationAsync_ValidCodeData_ReturnsGeneratedDocumentation()
        {
            // Arrange
            var codeData = new CodeDocumentationData
            {
                Id = "test-doc-1",
                Code = "public class UserService { public void CreateUser() { } }",
                Language = "C#",
                DocumentationType = "API Documentation",
                Style = "XML Comments",
                IncludeExamples = true,
                IncludeDiagrams = false
            };

            var mockResponse = new ModelResponse
            {
                Content = "Documentation generated successfully. Type: API Documentation, Style: XML Comments, Examples: 3, Diagrams: 0, Pages: 5, Sections: 8.",
                Success = true
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _advancedAIService.GenerateDocumentationAsync(codeData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Successfully generated documentation", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.Equal(codeData.Language, result.Language);
            Assert.Equal(codeData.DocumentationType, result.DocumentationType);
            Assert.Equal(codeData.Style, result.Style);
            Assert.True(result.ExampleCount > 0);
            Assert.True(result.DiagramCount >= 0);
            Assert.True(result.PageCount > 0);
            Assert.True(result.SectionCount > 0);
            Assert.NotNull(result.Documentation);
            Assert.NotNull(result.Metrics);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task ProcessNaturalLanguageAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var nlpRequest = new NLPRequest
            {
                Id = "test-nlp-error",
                Text = "Error text"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _advancedAIService.ProcessNaturalLanguageAsync(nlpRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(nlpRequest.Id, result.RequestId);
            Assert.True(result.ProcessedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateIntelligentCodeAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var codeRequest = new IntelligentCodeRequest
            {
                Id = "test-code-error",
                Description = "Error description"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _advancedAIService.GenerateIntelligentCodeAsync(codeRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(codeRequest.Id, result.RequestId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task OptimizeCodeAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var codeData = new CodeOptimizationData
            {
                Id = "test-optimization-error",
                Code = "Error code"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _advancedAIService.OptimizeCodeAsync(codeData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.True(result.OptimizedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task AnalyzeCodeQualityAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var codeData = new CodeQualityData
            {
                Id = "test-quality-error",
                Code = "Error code"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _advancedAIService.AnalyzeCodeQualityAsync(codeData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.True(result.AnalyzedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task GenerateDocumentationAsync_ModelOrchestratorThrows_ReturnsFailureResult()
        {
            // Arrange
            var codeData = new CodeDocumentationData
            {
                Id = "test-doc-error",
                Code = "Error code"
            };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Model orchestrator error"));

            // Act
            var result = await _advancedAIService.GenerateDocumentationAsync(codeData);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Model orchestrator error", result.Message);
            Assert.Equal(codeData.Id, result.CodeId);
            Assert.True(result.GeneratedAt > DateTimeOffset.MinValue);
        }

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

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
