using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Extensions;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;
using Xunit;

namespace Nexo.Feature.AI.Tests.RAG
{
    /// <summary>
    /// Performance tests for the RAG system
    /// </summary>
    public class RAGPerformanceTests : IClassFixture<RAGPerformanceTestFixture>
    {
        private readonly RAGPerformanceTestFixture _fixture;

        public RAGPerformanceTests(RAGPerformanceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task RAGSystem_BulkIndexing_PerformsWithinTimeLimit()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(1000); // 1000 chunks

            // Act
            var stopwatch = Stopwatch.StartNew();
            await ragService.BulkIndexDocumentationAsync(chunks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 30000); // Should complete within 30 seconds
        }

        [Fact]
        public async Task RAGSystem_QueryPerformance_PerformsWithinTimeLimit()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(500);
            await ragService.BulkIndexDocumentationAsync(chunks);

            var query = new RAGQuery
            {
                Query = "How do I use async await in C#?",
                ContextType = DocumentationContextType.CodeGeneration,
                MaxResults = 10,
                SimilarityThreshold = 0.5
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var response = await ragService.QueryDocumentationAsync(query);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(response);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task RAGSystem_ConcurrentQueries_PerformsWithinTimeLimit()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(1000);
            await ragService.BulkIndexDocumentationAsync(chunks);

            var queries = GenerateTestQueries(50); // 50 concurrent queries

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = queries.Select(q => ragService.QueryDocumentationAsync(q));
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(responses);
            Assert.Equal(50, responses.Length);
            Assert.True(stopwatch.ElapsedMilliseconds < 30000); // Should complete within 30 seconds
            Assert.All(responses, r => Assert.NotNull(r));
        }

        [Fact]
        public async Task RAGSystem_MemoryUsage_StaysWithinReasonableBounds()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(5000); // 5000 chunks

            // Act
            var beforeMemory = GC.GetTotalMemory(true);
            await ragService.BulkIndexDocumentationAsync(chunks);
            var afterMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryIncrease = afterMemory - beforeMemory;
            Assert.True(memoryIncrease < 100 * 1024 * 1024); // Should use less than 100MB
        }

        [Theory]
        [InlineData(100, 1000)] // 100 chunks, 1000 queries
        [InlineData(500, 500)]  // 500 chunks, 500 queries
        [InlineData(1000, 100)] // 1000 chunks, 100 queries
        public async Task RAGSystem_Scalability_HandlesDifferentLoads(int chunkCount, int queryCount)
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(chunkCount);
            await ragService.BulkIndexDocumentationAsync(chunks);

            var queries = GenerateTestQueries(queryCount);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = queries.Select(q => ragService.QueryDocumentationAsync(q));
            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(responses);
            Assert.Equal(queryCount, responses.Length);
            Assert.True(stopwatch.ElapsedMilliseconds < 60000); // Should complete within 60 seconds
        }

        [Fact]
        public async Task RAGSystem_EmbeddingGeneration_PerformsWithinTimeLimit()
        {
            // Arrange
            var embeddingService = _fixture.ServiceProvider.GetRequiredService<IDocumentationEmbeddingService>();
            var texts = GenerateTestTexts(1000); // 1000 texts

            // Act
            var stopwatch = Stopwatch.StartNew();
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(embeddings);
            Assert.Equal(1000, embeddings.Count());
            Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Should complete within 10 seconds
        }

        [Fact]
        public async Task RAGSystem_VectorSearch_PerformsWithinTimeLimit()
        {
            // Arrange
            var vectorStore = _fixture.ServiceProvider.GetRequiredService<IDocumentationVectorStore>();
            var chunks = GenerateTestChunks(1000);
            
            // Store chunks
            foreach (var chunk in chunks)
            {
                var embedding = new float[100];
                for (int i = 0; i < 100; i++)
                {
                    embedding[i] = (float)Random.Shared.NextDouble();
                }
                await vectorStore.StoreAsync(chunk, embedding);
            }

            var queryEmbedding = new float[100];
            for (int i = 0; i < 100; i++)
            {
                queryEmbedding[i] = (float)Random.Shared.NextDouble();
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var results = await vectorStore.SearchSimilarAsync(
                queryEmbedding, 
                maxResults: 10, 
                similarityThreshold: 0.5,
                filters: new List<DocumentationFilter>());
            stopwatch.Stop();

            // Assert
            Assert.NotNull(results);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete within 1 second
        }

        [Fact]
        public async Task RAGSystem_FilteredSearch_PerformsWithinTimeLimit()
        {
            // Arrange
            var ragService = _fixture.ServiceProvider.GetRequiredService<IDocumentationRAGService>();
            var chunks = GenerateTestChunks(1000);
            await ragService.BulkIndexDocumentationAsync(chunks);

            var query = new RAGQuery
            {
                Query = "C# programming",
                ContextType = DocumentationContextType.General,
                MaxResults = 10,
                SimilarityThreshold = 0.5,
                Filters = new List<DocumentationFilter>
                {
                    new() { Field = "Version", Value = "8.0", Operator = FilterOperator.Equals },
                    new() { Field = "Runtime", Value = "Net5Plus", Operator = FilterOperator.Equals }
                }
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var response = await ragService.QueryDocumentationAsync(query);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(response);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }

        private List<DocumentationChunk> GenerateTestChunks(int count)
        {
            var chunks = new List<DocumentationChunk>();
            var random = new Random(42); // Fixed seed for reproducible tests

            var topics = new[]
            {
                "async await", "LINQ", "dependency injection", "entity framework",
                "ASP.NET Core", "minimal APIs", "blazor", "signalr",
                "unit testing", "integration testing", "performance optimization",
                "memory management", "threading", "parallel programming"
            };

            var versions = new[] { "6.0", "7.0", "8.0" };
            var runtimes = new[] { "NetFramework", "NetCore", "Net5Plus" };
            var types = new[] { "Language", "Framework", "API", "Troubleshooting" };

            for (int i = 0; i < count; i++)
            {
                var topic = topics[random.Next(topics.Length)];
                var version = versions[random.Next(versions.Length)];
                var runtime = runtimes[random.Next(runtimes.Length)];
                var docType = types[random.Next(types.Length)];

                chunks.Add(new DocumentationChunk
                {
                    Id = $"test-{i}",
                    Title = $"{topic} in C# {version}",
                    Content = $"This is documentation about {topic} in C# {version} for {runtime}. " +
                             $"It covers various aspects of {topic} including best practices, examples, and common patterns. " +
                             $"This content is designed to help developers understand and implement {topic} effectively.",
                    DocumentationType = docType,
                    Version = version,
                    Runtime = runtime,
                    Tags = new List<string> { topic.Replace(" ", "-"), "csharp", version, runtime.ToLower() },
                    Categories = new List<string> { docType, "Programming", "Development" }
                });
            }

            return chunks;
        }

        private List<RAGQuery> GenerateTestQueries(int count)
        {
            var queries = new List<RAGQuery>();
            var random = new Random(42); // Fixed seed for reproducible tests

            var queryTemplates = new[]
            {
                "How do I use {0} in C#?",
                "What is {0} and how does it work?",
                "Show me examples of {0}",
                "Best practices for {0}",
                "Troubleshooting {0} issues",
                "Performance optimization with {0}",
                "Migration guide for {0}",
                "Advanced {0} patterns"
            };

            var topics = new[]
            {
                "async await", "LINQ", "dependency injection", "entity framework",
                "ASP.NET Core", "minimal APIs", "blazor", "signalr",
                "unit testing", "integration testing", "performance optimization"
            };

            var contextTypes = new[]
            {
                DocumentationContextType.General,
                DocumentationContextType.CodeGeneration,
                DocumentationContextType.ProblemSolving,
                DocumentationContextType.APIReference,
                DocumentationContextType.PerformanceOptimization
            };

            for (int i = 0; i < count; i++)
            {
                var topic = topics[random.Next(topics.Length)];
                var template = queryTemplates[random.Next(queryTemplates.Length)];
                var contextType = contextTypes[random.Next(contextTypes.Length)];

                queries.Add(new RAGQuery
                {
                    Query = string.Format(template, topic),
                    ContextType = contextType,
                    MaxResults = random.Next(5, 15),
                    SimilarityThreshold = 0.3 + (random.NextDouble() * 0.4) // 0.3 to 0.7
                });
            }

            return queries;
        }

        private List<string> GenerateTestTexts(int count)
        {
            var texts = new List<string>();
            var random = new Random(42); // Fixed seed for reproducible tests

            var words = new[]
            {
                "async", "await", "linq", "lambda", "delegate", "property", "method",
                "class", "interface", "namespace", "using", "public", "private",
                "static", "virtual", "override", "abstract", "sealed", "partial",
                "dependency", "injection", "entity", "framework", "aspnet", "core",
                "minimal", "api", "blazor", "signalr", "testing", "unit", "integration",
                "performance", "optimization", "memory", "threading", "parallel"
            };

            for (int i = 0; i < count; i++)
            {
                var wordCount = random.Next(10, 50);
                var textWords = new List<string>();
                
                for (int j = 0; j < wordCount; j++)
                {
                    textWords.Add(words[random.Next(words.Length)]);
                }
                
                texts.Add(string.Join(" ", textWords));
            }

            return texts;
        }
    }

    /// <summary>
    /// Test fixture for RAG performance tests
    /// </summary>
    public class RAGPerformanceTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public RAGPerformanceTestFixture()
        {
            var services = new ServiceCollection();
            
            // Add logging with minimal verbosity for performance tests
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));
            
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
