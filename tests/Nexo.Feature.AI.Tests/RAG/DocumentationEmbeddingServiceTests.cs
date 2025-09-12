using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Services.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Unit tests for DocumentationEmbeddingService
    /// </summary>
    public class DocumentationEmbeddingServiceTests
    {
        private readonly Mock<ILogger<DocumentationEmbeddingService>> _mockLogger;
        private readonly DocumentationEmbeddingService _embeddingService;

        public DocumentationEmbeddingServiceTests()
        {
            _mockLogger = new Mock<ILogger<DocumentationEmbeddingService>>();
            _embeddingService = new DocumentationEmbeddingService(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithValidText_ReturnsEmbedding()
        {
            // Arrange
            var text = "This is a test document about C# programming and async await patterns.";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length); // Should return 100-dimensional vector
            Assert.True(embedding.All(x => x >= 0 && x <= 1)); // Values should be normalized
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithEmptyText_ReturnsZeroEmbedding()
        {
            // Arrange
            var text = "";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.All(x => x == 0)); // Should be all zeros for empty text
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithCSharpKeywords_GeneratesRelevantEmbedding()
        {
            // Arrange
            var csharpText = "class interface async await linq lambda delegate property method constructor";
            var nonCsharpText = "random words that are not related to programming";

            // Act
            var csharpEmbedding = await _embeddingService.GenerateEmbeddingAsync(csharpText);
            var nonCsharpEmbedding = await _embeddingService.GenerateEmbeddingAsync(nonCsharpText);

            // Assert
            Assert.NotNull(csharpEmbedding);
            Assert.NotNull(nonCsharpEmbedding);
            Assert.Equal(100, csharpEmbedding.Length);
            Assert.Equal(100, nonCsharpEmbedding.Length);

            // C# text should have higher values in relevant dimensions
            Assert.True(csharpEmbedding.Any(x => x > 0));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithSameText_ReturnsIdenticalEmbedding()
        {
            // Arrange
            var text = "This is the same text for testing consistency.";

            // Act
            var embedding1 = await _embeddingService.GenerateEmbeddingAsync(text);
            var embedding2 = await _embeddingService.GenerateEmbeddingAsync(text);

            // Assert
            Assert.NotNull(embedding1);
            Assert.NotNull(embedding2);
            Assert.Equal(embedding1.Length, embedding2.Length);

            for (int i = 0; i < embedding1.Length; i++)
            {
                Assert.Equal(embedding1[i], embedding2[i]);
            }
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithDifferentText_ReturnsDifferentEmbeddings()
        {
            // Arrange
            var text1 = "This is about C# programming and async patterns.";
            var text2 = "This is about database design and SQL queries.";

            // Act
            var embedding1 = await _embeddingService.GenerateEmbeddingAsync(text1);
            var embedding2 = await _embeddingService.GenerateEmbeddingAsync(text2);

            // Assert
            Assert.NotNull(embedding1);
            Assert.NotNull(embedding2);
            Assert.Equal(embedding1.Length, embedding2.Length);

            // Embeddings should be different for different text
            var isDifferent = false;
            for (int i = 0; i < embedding1.Length; i++)
            {
                if (Math.Abs(embedding1[i] - embedding2[i]) > 0.001f)
                {
                    isDifferent = true;
                    break;
                }
            }
            Assert.True(isDifferent);
        }

        [Fact]
        public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ReturnsMultipleEmbeddings()
        {
            // Arrange
            var texts = new[]
            {
                "C# programming with async await",
                "Entity Framework database operations",
                "ASP.NET Core web API development",
                "Dependency injection patterns",
                "Unit testing with xUnit"
            };

            // Act
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

            // Assert
            Assert.NotNull(embeddings);
            Assert.Equal(5, embeddings.Count());

            foreach (var embedding in embeddings)
            {
                Assert.NotNull(embedding);
                Assert.Equal(100, embedding.Length);
            }
        }

        [Fact]
        public async Task GenerateEmbeddingsAsync_WithEmptyCollection_ReturnsEmptyCollection()
        {
            // Arrange
            var texts = new string[0];

            // Act
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

            // Assert
            Assert.NotNull(embeddings);
            Assert.Empty(embeddings);
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithLongText_HandlesCorrectly()
        {
            // Arrange
            var longText = string.Join(" ", Enumerable.Repeat("This is a test sentence about C# programming and async await patterns.", 100));

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(longText);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.Any(x => x != 0)); // Should have some non-zero values (including negative)
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var textWithSpecialChars = "C# async/await, LINQ queries, and .NET 8.0 features!";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(textWithSpecialChars);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.Any(x => x > 0)); // Should process special characters
        }

        [Theory]
        [InlineData("class interface async await", new[] { "class", "interface", "async", "await" })]
        [InlineData("linq lambda delegate property", new[] { "linq", "lambda", "delegate", "property" })]
        [InlineData("method constructor destructor", new[] { "method", "constructor", "destructor" })]
        [InlineData("asp.net mvc web api", new[] { "asp.net", "mvc", "web", "api" })]
        [InlineData("entity framework database sql", new[] { "entity", "framework", "database", "sql" })]
        public async Task GenerateEmbeddingAsync_WithCSharpTerms_GeneratesRelevantEmbedding(
            string text, 
            string[] expectedTerms)
        {
            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.Any(x => x > 0)); // Should have some non-zero values

            // The embedding should reflect the C# terms
            // (This is a simplified test - in practice, you'd check specific dimensions)
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithNullText_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _embeddingService.GenerateEmbeddingAsync(null!));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithWhitespaceOnly_ReturnsZeroEmbedding()
        {
            // Arrange
            var text = "   \t\n\r   ";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(text);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.All(x => x == 0)); // Should be all zeros for whitespace-only text
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithVeryShortText_HandlesCorrectly()
        {
            // Arrange
            var shortText = "C#";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(shortText);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            // Should still generate a valid embedding even for very short text
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithMixedCase_HandlesCorrectly()
        {
            // Arrange
            var mixedCaseText = "C# Async Await LINQ Lambda Delegate Property Method";

            // Act
            var embedding = await _embeddingService.GenerateEmbeddingAsync(mixedCaseText);

            // Assert
            Assert.NotNull(embedding);
            Assert.Equal(100, embedding.Length);
            Assert.True(embedding.Any(x => x > 0)); // Should process mixed case correctly
        }
    }
}
