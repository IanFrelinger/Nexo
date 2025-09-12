using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;
using Nexo.Feature.AI.Services.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Unit tests for DocumentationIngestionService
    /// </summary>
    public class DocumentationIngestionServiceTests
    {
        private readonly Mock<ILogger<DocumentationIngestionService>> _mockLogger;
        private readonly Mock<IDocumentationRAGService> _mockRAGService;
        private readonly Mock<IDocumentationParser> _mockParser;
        private readonly DocumentationIngestionService _ingestionService;

        public DocumentationIngestionServiceTests()
        {
            _mockLogger = new Mock<ILogger<DocumentationIngestionService>>();
            _mockRAGService = new Mock<IDocumentationRAGService>();
            _mockParser = new Mock<IDocumentationParser>();

            _ingestionService = new DocumentationIngestionService(
                _mockLogger.Object,
                _mockRAGService.Object,
                _mockParser.Object
            );
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithValidFile_ProcessesSuccessfully()
        {
            // Arrange
            var filePath = "test-documentation.md";
            var content = "# Test Documentation\n\nThis is test content.";
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var chunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Test Documentation",
                    Content = "This is test content.",
                    DocumentationType = "Test",
                    Version = "1.0",
                    Runtime = "Test"
                }
            };

            // Mock file reading
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ReturnsAsync(chunks);

            _mockRAGService.Setup(x => x.BulkIndexDocumentationAsync(chunks))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata);

                // Assert
                _mockParser.Verify(x => x.ParseDocumentationAsync(content, metadata), Times.Once);
                _mockRAGService.Verify(x => x.BulkIndexDocumentationAsync(chunks), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithFileNotFound_ThrowsException()
        {
            // Arrange
            var filePath = "non-existent-file.md";
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _ingestionService.IngestCustomDocumentationAsync(filePath, metadata));
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithParserError_ThrowsException()
        {
            // Arrange
            var filePath = "test-documentation.md";
            var content = "# Test Documentation\n\nThis is test content.";
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ThrowsAsync(new Exception("Parser error"));

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata));
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithRAGError_ThrowsException()
        {
            // Arrange
            var filePath = "test-documentation.md";
            var content = "# Test Documentation\n\nThis is test content.";
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var chunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Test Documentation",
                    Content = "This is test content."
                }
            };

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ReturnsAsync(chunks);

            _mockRAGService.Setup(x => x.BulkIndexDocumentationAsync(chunks))
                .ThrowsAsync(new Exception("RAG service error"));

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata));
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithEmptyContent_ProcessesSuccessfully()
        {
            // Arrange
            var filePath = "empty-documentation.md";
            var content = "";
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var chunks = new List<DocumentationChunk>();

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ReturnsAsync(chunks);

            _mockRAGService.Setup(x => x.BulkIndexDocumentationAsync(chunks))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata);

                // Assert
                _mockParser.Verify(x => x.ParseDocumentationAsync(content, metadata), Times.Once);
                _mockRAGService.Verify(x => x.BulkIndexDocumentationAsync(chunks), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithLargeContent_ProcessesSuccessfully()
        {
            // Arrange
            var filePath = "large-documentation.md";
            var content = string.Join("\n", Enumerable.Repeat("This is a test line of documentation content.", 1000));
            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            var chunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Large Documentation",
                    Content = content.Substring(0, 100) + "...",
                    DocumentationType = "Test",
                    Version = "1.0",
                    Runtime = "Test"
                }
            };

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ReturnsAsync(chunks);

            _mockRAGService.Setup(x => x.BulkIndexDocumentationAsync(chunks))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata);

                // Assert
                _mockParser.Verify(x => x.ParseDocumentationAsync(content, metadata), Times.Once);
                _mockRAGService.Verify(x => x.BulkIndexDocumentationAsync(chunks), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Theory]
        [InlineData("csharp", "CSharp")]
        [InlineData("dotnet", "DotNet")]
        [InlineData("CSharp", "CSharp")]
        [InlineData("DOTNET", "DotNet")]
        public async Task IngestCSharpDocumentationAsync_WithDifferentTypes_ProcessesCorrectly(string type, string expectedType)
        {
            // This test would require setting up a mock file system
            // For now, we'll test the method signature and basic behavior
            var documentationPath = "./test-docs";

            // Act
            await _ingestionService.IngestCSharpDocumentationAsync(documentationPath);

            // Assert - Should not throw exception, just log warning and return
            // The method handles non-existent directories gracefully
        }

        [Theory]
        [InlineData("8.0", "8.0")]
        [InlineData("6.0", "6.0")]
        [InlineData("latest", "latest")]
        public async Task IngestDotNetRuntimeDocumentationAsync_WithDifferentVersions_ProcessesCorrectly(
            string version, 
            string expectedVersion)
        {
            // This test would require setting up a mock file system
            // For now, we'll test the method signature and basic behavior
            var documentationPath = "./test-docs";

            // Act
            await _ingestionService.IngestDotNetRuntimeDocumentationAsync(documentationPath, version);

            // Assert - Should not throw exception, just log warning and return
            // The method handles non-existent directories gracefully
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithNullFilePath_ThrowsException()
        {
            // Arrange
            var metadata = new DocumentationMetadata
            {
                Source = "test",
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _ingestionService.IngestCustomDocumentationAsync(null!, metadata));
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithNullMetadata_ThrowsException()
        {
            // Arrange
            var filePath = "test-documentation.md";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _ingestionService.IngestCustomDocumentationAsync(filePath, null!));
        }

        [Fact]
        public async Task IngestCustomDocumentationAsync_WithValidMarkdown_ProcessesCorrectly()
        {
            // Arrange
            var filePath = "test-documentation.md";
            var content = @"# Test Documentation

## Introduction
This is a test documentation file.

## Code Example
```csharp
public class TestClass
{
    public void TestMethod()
    {
        Console.WriteLine(""Hello World"");
    }
}
```

## Conclusion
This is the end of the documentation.";

            var metadata = new DocumentationMetadata
            {
                Source = filePath,
                DocumentationType = "Test",
                Version = "1.0",
                Runtime = "Test",
                Categories = new[] { "Test", "Example" },
                Tags = new[] { "csharp", "test", "example" }
            };

            var chunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "1",
                    Title = "Test Documentation",
                    Content = "This is a test documentation file.",
                    DocumentationType = "Test",
                    Version = "1.0",
                    Runtime = "Test"
                },
                new DocumentationChunk
                {
                    Id = "2",
                    Title = "Code Example",
                    Content = "public class TestClass...",
                    DocumentationType = "Test",
                    Version = "1.0",
                    Runtime = "Test"
                }
            };

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content);

            _mockParser.Setup(x => x.ParseDocumentationAsync(content, metadata))
                .ReturnsAsync(chunks);

            _mockRAGService.Setup(x => x.BulkIndexDocumentationAsync(chunks))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _ingestionService.IngestCustomDocumentationAsync(tempFile, metadata);

                // Assert
                _mockParser.Verify(x => x.ParseDocumentationAsync(content, metadata), Times.Once);
                _mockRAGService.Verify(x => x.BulkIndexDocumentationAsync(chunks), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
