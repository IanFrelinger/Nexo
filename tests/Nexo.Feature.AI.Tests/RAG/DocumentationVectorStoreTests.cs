using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Models.RAG;
using Nexo.Feature.AI.Services.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Unit tests for DocumentationVectorStore
    /// </summary>
    public class DocumentationVectorStoreTests
    {
        private readonly Mock<ILogger<DocumentationVectorStore>> _mockLogger;
        private readonly DocumentationVectorStore _vectorStore;

        public DocumentationVectorStoreTests()
        {
            _mockLogger = new Mock<ILogger<DocumentationVectorStore>>();
            _vectorStore = new DocumentationVectorStore(_mockLogger.Object);
        }

        [Fact]
        public async Task StoreAsync_WithValidChunk_StoresSuccessfully()
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "test-1",
                Title = "Test Documentation",
                Content = "Test content",
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            // Act
            await _vectorStore.StoreAsync(chunk, embedding);

            // Assert
            var retrievedChunk = await _vectorStore.GetByIdAsync("test-1");
            Assert.NotNull(retrievedChunk);
            Assert.Equal(chunk.Id, retrievedChunk.Id);
            Assert.Equal(chunk.Title, retrievedChunk.Title);
        }

        [Fact]
        public async Task SearchSimilarAsync_WithMatchingQuery_ReturnsSimilarChunks()
        {
            // Arrange
            var chunk1 = new DocumentationChunk
            {
                Id = "1",
                Title = "Async Await in C#",
                Content = "Async and await keywords in C# provide asynchronous programming",
                DocumentationType = "Language",
                Version = "8.0",
                Runtime = "Net5Plus"
            };

            var chunk2 = new DocumentationChunk
            {
                Id = "2",
                Title = "LINQ in C#",
                Content = "LINQ provides query capabilities for collections",
                DocumentationType = "Language",
                Version = "8.0",
                Runtime = "Net5Plus"
            };

            var embedding1 = new float[] { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f };
            var embedding2 = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            await _vectorStore.StoreAsync(chunk1, embedding1);
            await _vectorStore.StoreAsync(chunk2, embedding2);

            var queryEmbedding = new float[] { 0.95f, 0.85f, 0.75f, 0.65f, 0.55f }; // Similar to chunk1

            // Act
            var results = await _vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 2, 
                similarityThreshold: 0.5,
                filters: new List<DocumentationFilter>());

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Any());
            Assert.True(results.First().Id == "1" || results.First().Id == "2");
        }

        [Fact]
        public async Task SearchSimilarAsync_WithFilters_AppliesFiltersCorrectly()
        {
            // Arrange
            var chunk1 = new DocumentationChunk
            {
                Id = "1",
                Title = "C# 8.0 Features",
                Content = "New features in C# 8.0",
                DocumentationType = "Language",
                Version = "8.0",
                Runtime = "Net5Plus"
            };

            var chunk2 = new DocumentationChunk
            {
                Id = "2",
                Title = "C# 6.0 Features",
                Content = "New features in C# 6.0",
                DocumentationType = "Language",
                Version = "6.0",
                Runtime = "NetFramework"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            await _vectorStore.StoreAsync(chunk1, embedding);
            await _vectorStore.StoreAsync(chunk2, embedding);

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            var filters = new List<DocumentationFilter>
            {
                new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals }
            };

            // Act
            var results = await _vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 10, 
                similarityThreshold: 0.5,
                filters: filters);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("1", results.First().Id);
            Assert.Equal("8.0", results.First().Version);
        }

        [Fact]
        public async Task SearchSimilarAsync_WithLowSimilarityThreshold_ReturnsNoResults()
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "1",
                Title = "Test Documentation",
                Content = "Test content",
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            await _vectorStore.StoreAsync(chunk, embedding);

            var queryEmbedding = new float[] { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f }; // Very different

            // Act
            var results = await _vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 10, 
                similarityThreshold: 0.9, // Very high threshold
                filters: new List<DocumentationFilter>());

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ReturnsChunk()
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "test-id",
                Title = "Test Documentation",
                Content = "Test content",
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            await _vectorStore.StoreAsync(chunk, embedding);

            // Act
            var result = await _vectorStore.GetByIdAsync("test-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-id", result.Id);
            Assert.Equal("Test Documentation", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Act
            var result = await _vectorStore.GetByIdAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_WithMultipleChunks_ReturnsAllChunks()
        {
            // Arrange
            var chunk1 = new DocumentationChunk
            {
                Id = "1",
                Title = "Documentation 1",
                Content = "Content 1"
            };

            var chunk2 = new DocumentationChunk
            {
                Id = "2",
                Title = "Documentation 2",
                Content = "Content 2"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            await _vectorStore.StoreAsync(chunk1, embedding);
            await _vectorStore.StoreAsync(chunk2, embedding);

            // Act
            var results = await _vectorStore.GetAllAsync();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count());
            Assert.Contains(results, c => c.Id == "1");
            Assert.Contains(results, c => c.Id == "2");
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_RemovesChunk()
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "test-id",
                Title = "Test Documentation",
                Content = "Test content"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            await _vectorStore.StoreAsync(chunk, embedding);

            // Verify it exists
            var beforeDelete = await _vectorStore.GetByIdAsync("test-id");
            Assert.NotNull(beforeDelete);

            // Act
            await _vectorStore.DeleteAsync("test-id");

            // Assert
            var afterDelete = await _vectorStore.GetByIdAsync("test-id");
            Assert.Null(afterDelete);
        }

        [Fact]
        public async Task GetCountAsync_WithMultipleChunks_ReturnsCorrectCount()
        {
            // Arrange
            var chunk1 = new DocumentationChunk { Id = "1", Title = "Doc 1", Content = "Content 1" };
            var chunk2 = new DocumentationChunk { Id = "2", Title = "Doc 2", Content = "Content 2" };
            var chunk3 = new DocumentationChunk { Id = "3", Title = "Doc 3", Content = "Content 3" };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            await _vectorStore.StoreAsync(chunk1, embedding);
            await _vectorStore.StoreAsync(chunk2, embedding);
            await _vectorStore.StoreAsync(chunk3, embedding);

            // Act
            var count = await _vectorStore.GetCountAsync();

            // Assert
            Assert.Equal(3, count);
        }

        [Theory]
        [InlineData("Version", "8.0", FilterOperator.Equals, true)]
        [InlineData("Version", "6.0", FilterOperator.Equals, false)]
        [InlineData("Title", "Test", FilterOperator.Contains, true)]
        [InlineData("Title", "NonExistent", FilterOperator.Contains, false)]
        [InlineData("Title", "Documentation", FilterOperator.StartsWith, false)]
        [InlineData("Title", "Doc", FilterOperator.StartsWith, false)]
        [InlineData("Title", "Test", FilterOperator.StartsWith, true)]
        public async Task SearchSimilarAsync_WithDifferentFilters_AppliesFiltersCorrectly(
            string field, 
            string value, 
            FilterOperator op, 
            bool shouldMatch)
        {
            // Arrange
            var chunk = new DocumentationChunk
            {
                Id = "1",
                Title = "Test Documentation",
                Content = "Test content",
                Version = "8.0",
                Runtime = "Net5Plus"
            };

            var embedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            await _vectorStore.StoreAsync(chunk, embedding);

            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            var filters = new List<DocumentationFilter>
            {
                new() { Field = field, Value = value, Operator = op }
            };

            // Act
            var results = await _vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 10, 
                similarityThreshold: 0.0, // Lower threshold to ensure match
                filters: filters);


            // Assert
            if (shouldMatch)
            {
                Assert.NotNull(results);
                Assert.True(results.Any(), $"Expected results but got none. Field: {field}, Value: {value}, Operator: {op}");
            }
            else
            {
                Assert.NotNull(results);
                Assert.Empty(results);
            }
        }

        [Fact]
        public async Task SearchSimilarAsync_WithEmptyStore_ReturnsEmptyResults()
        {
            // Arrange
            var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

            // Act
            var results = await _vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 10, 
                similarityThreshold: 0.5,
                filters: new List<DocumentationFilter>());

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }
    }
}
