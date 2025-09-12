using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Models.RAG;
using Nexo.Feature.AI.Services.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Unit tests for DocumentationRAGService
    /// </summary>
    public class DocumentationRAGServiceTests
    {
        private readonly Mock<ILogger<DocumentationRAGService>> _mockLogger;
        private readonly Mock<IDocumentationVectorStore> _mockVectorStore;
        private readonly Mock<IDocumentationEmbeddingService> _mockEmbeddingService;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
        private readonly DocumentationRAGService _ragService;

        public DocumentationRAGServiceTests()
        {
            _mockLogger = new Mock<ILogger<DocumentationRAGService>>();
            _mockVectorStore = new Mock<IDocumentationVectorStore>();
            _mockEmbeddingService = new Mock<IDocumentationEmbeddingService>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();

            _ragService = new DocumentationRAGService(
                _mockLogger.Object,
                _mockVectorStore.Object,
                _mockEmbeddingService.Object,
                _mockModelOrchestrator.Object
            );
        }

        [Fact]
        public async Task QueryDocumentationAsync_WithValidQuery_ReturnsRAGResponse()
        {
            // Arrange
            var query = new RAGQuery
            {
                Query = "How do I use async await in C#?",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 5,
                SimilarityThreshold = 0.7
            };

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var mockChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Async Await in C#",
                    Content = "Async and await keywords in C# provide a way to write asynchronous code...",
                    DocumentationType = "Language",
                    Version = "8.0",
                    Runtime = "Net5Plus",
                    RelevanceScore = 0.9
                }
            };

            var mockAIResponse = new ModelResponse
            {
                Response = "Async and await in C# allow you to write asynchronous code that looks synchronous...",
                Success = true
            };

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(query.Query))
                .ReturnsAsync(queryEmbedding);

            _mockVectorStore.Setup(x => x.SearchSimilarAsync(
                queryEmbedding, 
                query.MaxResults, 
                query.SimilarityThreshold, 
                query.Filters))
                .ReturnsAsync(mockChunks);

            _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAIResponse);

            // Act
            var result = await _ragService.QueryDocumentationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(query.Query, result.Query);
            Assert.NotNull(result.AIResponse);
            Assert.True(result.RetrievedChunks.Any());
            Assert.True(result.ConfidenceScore > 0);
        }

        [Fact]
        public async Task QueryDocumentationAsync_WithNoResults_ReturnsEmptyResponse()
        {
            // Arrange
            var query = new RAGQuery
            {
                Query = "Non-existent topic",
                ContextType = DocumentationContextType.General,
                MaxResults = 5,
                SimilarityThreshold = 0.9
            };

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var emptyChunks = new List<DocumentationChunk>();

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(query.Query))
                .ReturnsAsync(queryEmbedding);

            _mockVectorStore.Setup(x => x.SearchSimilarAsync(
                queryEmbedding, 
                query.MaxResults, 
                query.SimilarityThreshold, 
                query.Filters))
                .ReturnsAsync(emptyChunks);

            // Mock the model orchestrator for empty results
            _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModelResponse { Response = "No relevant documentation found." });

            // Act
            var result = await _ragService.QueryDocumentationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(query.Query, result.Query);
            Assert.False(result.RetrievedChunks.Any());
            Assert.Equal(0.0, result.ConfidenceScore);
        }

        [Fact]
        public async Task IndexDocumentationAsync_WithValidChunk_StoresSuccessfully()
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "test-1",
                Title = "Test Documentation",
                Content = "This is test documentation content.",
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(chunk.Content))
                .ReturnsAsync(embedding);

            _mockVectorStore.Setup(x => x.StoreAsync(chunk, embedding))
                .Returns(Task.CompletedTask);

            // Act
            await _ragService.IndexDocumentationAsync(chunk);

            // Assert
            _mockEmbeddingService.Verify(x => x.GenerateEmbeddingAsync(chunk.Content), Times.Once);
            _mockVectorStore.Verify(x => x.StoreAsync(chunk, embedding), Times.Once);
        }

        [Fact]
        public async Task BulkIndexDocumentationAsync_WithMultipleChunks_ProcessesAll()
        {
            // Arrange
            var chunks = new List<DocumentationChunk>
            {
                new DocumentationChunk { Id = "1", Title = "Test 1", Content = "Content 1" },
                new DocumentationChunk { Id = "2", Title = "Test 2", Content = "Content 2" },
                new DocumentationChunk { Id = "3", Title = "Test 3", Content = "Content 3" }
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(embedding);

            _mockVectorStore.Setup(x => x.StoreAsync(It.IsAny<DocumentationChunk>(), It.IsAny<float[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _ragService.BulkIndexDocumentationAsync(chunks);

            // Assert
            _mockEmbeddingService.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Exactly(3));
            _mockVectorStore.Verify(x => x.StoreAsync(It.IsAny<DocumentationChunk>(), It.IsAny<float[]>()), Times.Exactly(3));
        }

        [Fact]
        public async Task QueryDocumentationAsync_WithFilters_AppliesFiltersCorrectly()
        {
            // Arrange
            var query = new RAGQuery
            {
                Query = "C# async await",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 5,
                SimilarityThreshold = 0.7,
                Filters = new List<DocumentationFilter>
                {
                    new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals },
                    new() { Field = "Runtime", Value = "Net5Plus", Operator = FilterOperator.Equals }
                }
            };

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var mockChunks = new List<DocumentationChunk>();

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(query.Query))
                .ReturnsAsync(queryEmbedding);

            _mockVectorStore.Setup(x => x.SearchSimilarAsync(
                queryEmbedding, 
                query.MaxResults, 
                query.SimilarityThreshold, 
                query.Filters))
                .ReturnsAsync(mockChunks);

            // Mock the model orchestrator
            _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModelResponse { Response = "Filtered documentation response." });

            // Act
            await _ragService.QueryDocumentationAsync(query);

            // Assert
            _mockVectorStore.Verify(x => x.SearchSimilarAsync(
                queryEmbedding, 
                query.MaxResults, 
                query.SimilarityThreshold, 
                query.Filters), Times.Once);
        }

        [Theory]
        [InlineData(DocumentationContextType.CodeGeneration, "Code Generation")]
        [InlineData(DocumentationContextType.ProblemSolving, "Problem Solving")]
        [InlineData(DocumentationContextType.APIReference, "API Documentation")]
        [InlineData(DocumentationContextType.PerformanceOptimization, "Performance Considerations")]
        public async Task QueryDocumentationAsync_WithDifferentContextTypes_BuildsCorrectContext(
            DocumentationContextType contextType, 
            string expectedContextTitle)
        {
            // Arrange
            var query = new RAGQuery
            {
                Query = "Test query",
                ContextType = contextType,
                MaxResults = 5,
                SimilarityThreshold = 0.7
            };

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            var mockChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Test Documentation",
                    Content = "Test content",
                    Version = "8.0",
                    DocumentationType = "Test"
                }
            };

            var mockAIResponse = new ModelResponse
            {
                Response = "Test AI response",
                Success = true
            };

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(query.Query))
                .ReturnsAsync(queryEmbedding);

            _mockVectorStore.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), 
                It.IsAny<int>(), 
                It.IsAny<double>(), 
                It.IsAny<List<DocumentationFilter>>()))
                .ReturnsAsync(mockChunks);

            _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAIResponse);

            // Act
            var result = await _ragService.QueryDocumentationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectedContextTitle, result.Context);
        }

        [Fact]
        public async Task QueryDocumentationAsync_WithException_ThrowsException()
        {
            // Arrange
            var query = new RAGQuery
            {
                Query = "Test query",
                ContextType = DocumentationContextType.General,
                MaxResults = 5,
                SimilarityThreshold = 0.7
            };

            _mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(query.Query))
                .ThrowsAsync(new Exception("Embedding service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _ragService.QueryDocumentationAsync(query));
        }
    }
}
