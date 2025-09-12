using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Extensions;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;
using Nexo.Feature.AI.Services.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Integration tests for the RAG system
    /// </summary>
    public class RAGIntegrationTests : IClassFixture<RAGTestFixture>
    {
        private readonly RAGTestFixture _fixture;

        public RAGIntegrationTests(RAGTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task RAGSystem_EndToEnd_WorksCorrectly()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var ingestionService = _fixture.ServiceProvider.GetRequiredService<IDocumentationIngestionService>();

            // Create test documentation
            var testChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "test-1",
                    Title = "Async Await in C#",
                    Content = "Async and await keywords in C# provide a way to write asynchronous code that looks like synchronous code. This pattern is essential for writing responsive applications.",
                    DocumentationType = "Language",
                    Version = "8.0",
                    Runtime = "Net5Plus",
                    Tags = new List<string> { "async", "await", "csharp", "asynchronous" },
                    Categories = new List<string> { "Language", "Programming" }
                },
                new DocumentationChunk
                {
                    Id = "test-2",
                    Title = "LINQ in C#",
                    Content = "Language Integrated Query (LINQ) provides query capabilities for collections and data sources. It allows you to write queries using C# syntax.",
                    DocumentationType = "Language",
                    Version = "8.0",
                    Runtime = "Net5Plus",
                    Tags = new List<string> { "linq", "query", "csharp", "collections" },
                    Categories = new List<string> { "Language", "Data" }
                },
                new DocumentationChunk
                {
                    Id = "test-3",
                    Title = "Dependency Injection in ASP.NET Core",
                    Content = "Dependency injection is a design pattern that allows you to inject dependencies into your classes rather than creating them directly.",
                    DocumentationType = "Framework",
                    Version = "8.0",
                    Runtime = "Net5Plus",
                    Tags = new List<string> { "dependency-injection", "aspnet", "core", "di" },
                    Categories = new List<string> { "Framework", "Architecture" }
                }
            };

            // Ingest test documentation
            await ragService.BulkIndexDocumentationAsync(testChunks);

            // Act & Assert - Test async/await query
            var asyncQuery = new RAGQuery
            {
                Query = "How do I use async await in C#?",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 3,
                SimilarityThreshold = 0.5
            };

            var asyncResponse = await ragService.QueryDocumentationAsync(asyncQuery);
            Assert.NotNull(asyncResponse);
            Assert.True(asyncResponse.RetrievedChunks.Any());
            Assert.Contains(asyncResponse.RetrievedChunks, c => c.Title.Contains("Async Await"));
            Assert.True(asyncResponse.ConfidenceScore > 0);

            // Act & Assert - Test LINQ query
            var linqQuery = new RAGQuery
            {
                Query = "What is LINQ and how do I use it?",
                ContextType = DocumentationContextType.General,
                MaxResults = 3,
                SimilarityThreshold = 0.5
            };

            var linqResponse = await ragService.QueryDocumentationAsync(linqQuery);
            Assert.NotNull(linqResponse);
            Assert.True(linqResponse.RetrievedChunks.Any());
            Assert.Contains(linqResponse.RetrievedChunks, c => c.Title.Contains("LINQ"));
            Assert.True(linqResponse.ConfidenceScore > 0);

            // Act & Assert - Test dependency injection query
            var diQuery = new RAGQuery
            {
                Query = "How do I implement dependency injection?",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 3,
                SimilarityThreshold = 0.5
            };

            var diResponse = await ragService.QueryDocumentationAsync(diQuery);
            Assert.NotNull(diResponse);
            Assert.True(diResponse.RetrievedChunks.Any());
            Assert.Contains(diResponse.RetrievedChunks, c => c.Title.Contains("Dependency Injection"));
            Assert.True(diResponse.ConfidenceScore > 0);
        }

        [Fact]
        public async Task RAGSystem_WithFilters_WorksCorrectly()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();

            // Create test documentation with different versions
            var testChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "version-8-1",
                    Title = "C# 8.0 Features",
                    Content = "C# 8.0 introduced nullable reference types, async streams, and more.",
                    DocumentationType = "Language",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                },
                new DocumentationChunk
                {
                    Id = "version-6-1",
                    Title = "C# 6.0 Features",
                    Content = "C# 6.0 introduced string interpolation, null-conditional operators, and more.",
                    DocumentationType = "Language",
                    Version = "6.0",
                    Runtime = "NetFramework"
                },
                new DocumentationChunk
                {
                    Id = "version-8-2",
                    Title = "ASP.NET Core 8.0",
                    Content = "ASP.NET Core 8.0 includes minimal APIs, native AOT, and performance improvements.",
                    DocumentationType = "Framework",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                }
            };

            await ragService.BulkIndexDocumentationAsync(testChunks);

            // Act - Query with version filter
            var versionQuery = new RAGQuery
            {
                Query = "What are the new features?",
                ContextType = DocumentationContextType.General,
                MaxResults = 5,
                SimilarityThreshold = 0.0, // Lower threshold to ensure match
                Filters = new List<DocumentationFilter>
                {
                    new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals }
                }
            };

            var versionResponse = await ragService.QueryDocumentationAsync(versionQuery);

            // Assert
            Assert.NotNull(versionResponse);
            Assert.True(versionResponse.RetrievedChunks.Any());
            Assert.All(versionResponse.RetrievedChunks, c => Assert.Equal("8.0", c.Version));
        }

        [Fact]
        public async Task RAGSystem_WithDifferentContextTypes_ReturnsAppropriateContext()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();

            var testChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "code-gen-1",
                    Title = "Creating a REST API Controller",
                    Content = "Here's how to create a REST API controller in ASP.NET Core...",
                    DocumentationType = "Framework",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                },
                new DocumentationChunk
                {
                    Id = "problem-1",
                    Title = "Debugging Async Deadlocks",
                    Content = "Common causes of async deadlocks and how to fix them...",
                    DocumentationType = "Troubleshooting",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                }
            };

            await ragService.BulkIndexDocumentationAsync(testChunks);

            // Act & Assert - Code generation context
            var codeGenQuery = new RAGQuery
            {
                Query = "How do I create an API controller?",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 3,
                SimilarityThreshold = 0.3
            };

            var codeGenResponse = await ragService.QueryDocumentationAsync(codeGenQuery);
            Assert.NotNull(codeGenResponse);
            Assert.Contains("Code Generation", codeGenResponse.Context);

            // Act & Assert - Problem solving context
            var problemQuery = new RAGQuery
            {
                Query = "My async code is deadlocking",
                ContextType = DocumentationContextType.ProblemSolving,
                MaxResults = 3,
                SimilarityThreshold = 0.3
            };

            var problemResponse = await ragService.QueryDocumentationAsync(problemQuery);
            Assert.NotNull(problemResponse);
            Assert.Contains("Problem Solving", problemResponse.Context);
        }

        [Fact]
        public async Task RAGSystem_WithLowSimilarityThreshold_ReturnsNoResults()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();

            var testChunks = new List<DocumentationChunk>
            {
                new DocumentationChunk
                {
                    Id = "test-1",
                    Title = "C# Programming",
                    Content = "C# is a programming language developed by Microsoft.",
                    DocumentationType = "Language",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                }
            };

            await ragService.BulkIndexDocumentationAsync(testChunks);

            // Act
            var query = new RAGQuery
            {
                Query = "completely unrelated topic about cooking recipes",
                ContextType = DocumentationContextType.General,
                MaxResults = 5,
                SimilarityThreshold = 0.9 // Very high threshold
            };

            var response = await ragService.QueryDocumentationAsync(query);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.RetrievedChunks);
            Assert.Equal(0.0, response.ConfidenceScore);
        }

        [Fact]
        public async Task RAGSystem_WithEmptyQuery_HandlesGracefully()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();

            // Act
            var query = new RAGQuery
            {
                Query = "",
                ContextType = DocumentationContextType.General,
                MaxResults = 5,
                SimilarityThreshold = 0.5
            };

            var response = await ragService.QueryDocumentationAsync(query);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("", response.Query);
        }

        [Fact]
        public async Task RAGSystem_Performance_HandlesMultipleQueries()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();

            var testChunks = new List<DocumentationChunk>();
            for (int i = 0; i < 100; i++)
            {
                testChunks.Add(new DocumentationChunk
                {
                    Id = $"test-{i}",
                    Title = $"Test Documentation {i}",
                    Content = $"This is test documentation content {i} about C# programming and development.",
                    DocumentationType = "Test",
                    Version = "8.0",
                    Runtime = "Net5Plus"
                });
            }

            await ragService.BulkIndexDocumentationAsync(testChunks);

            // Act - Run multiple queries in parallel
            var queries = new[]
            {
                new RAGQuery { Query = "C# programming", ContextType = DocumentationContextType.General, MaxResults = 5, SimilarityThreshold = 0.5 },
                new RAGQuery { Query = "async await", ContextType = DocumentationContextType.CodeGeneration, MaxResults = 5, SimilarityThreshold = 0.5 },
                new RAGQuery { Query = "dependency injection", ContextType = DocumentationContextType.General, MaxResults = 5, SimilarityThreshold = 0.5 },
                new RAGQuery { Query = "LINQ queries", ContextType = DocumentationContextType.General, MaxResults = 5, SimilarityThreshold = 0.5 },
                new RAGQuery { Query = "error handling", ContextType = DocumentationContextType.ProblemSolving, MaxResults = 5, SimilarityThreshold = 0.5 }
            };

            var startTime = DateTime.UtcNow;
            var tasks = queries.Select(q => ragService.QueryDocumentationAsync(q));
            var responses = await Task.WhenAll(tasks);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.NotNull(responses);
            Assert.Equal(5, responses.Length);
            Assert.All(responses, r => Assert.NotNull(r));
            Assert.True((endTime - startTime).TotalSeconds < 10); // Should complete within 10 seconds
        }
    }

    /// <summary>
    /// Test fixture for RAG integration tests
    /// </summary>
    public class RAGTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public RAGTestFixture()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            // Add RAG services
            services.AddRAGServices();
            
            ServiceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
