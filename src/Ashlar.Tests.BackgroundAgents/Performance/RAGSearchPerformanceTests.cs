using FluentAssertions;
using Ashlar.BackgroundAgents.DataSensitivity;
using Ashlar.BackgroundAgents.RAG;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Performance;

/// <summary>
/// Performance-oriented tests for RAG search and indexing (in-memory store).
/// Asserts baseline expectations for latency with varying corpus sizes.
/// </summary>
public class RAGSearchPerformanceTests
{
    [Fact]
    public async Task RAG_Search_SmallCorpus_CompletesWithinReasonableTime()
    {
        var sensitivityRegistry = new DataSensitivityRegistry();
        var store = new InMemoryVectorStore(sensitivityRegistry);
        var embedding = new TokenEmbeddingGenerator();
        var rag = new RAGService(store, embedding);

        for (var i = 0; i < 50; i++)
            await rag.IndexAsync($"doc-{i}", $"Document content number {i} with some text.", "Public", default);

        var count = await rag.GetDocumentCountAsync(default);
        count.Should().Be(50);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 20; i++)
        {
            var results = await rag.SearchAsync("document content", 5, 0.0, "Public", default);
            results.Should().NotBeEmpty();
        }
        stopwatch.Stop();
        var avgMsPerSearch = stopwatch.ElapsedMilliseconds / 20.0;
        avgMsPerSearch.Should().BeLessThan(200, "search over 50 docs should complete in under 200ms per call");
    }

    [Fact]
    public async Task RAG_Index_Throughput_SustainsManyDocuments()
    {
        var sensitivityRegistry = new DataSensitivityRegistry();
        var store = new InMemoryVectorStore(sensitivityRegistry);
        var embedding = new TokenEmbeddingGenerator();
        var rag = new RAGService(store, embedding);

        const int docCount = 200;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < docCount; i++)
            await rag.IndexAsync($"doc-{i}", $"Content {i} with unique words here.", "Public", default);
        stopwatch.Stop();

        var count = await rag.GetDocumentCountAsync(default);
        count.Should().Be(docCount);
        var avgMsPerIndex = stopwatch.ElapsedMilliseconds / (double)docCount;
        avgMsPerIndex.Should().BeLessThan(50, "indexing should average under 50ms per document");
    }

    [Fact]
    public async Task RAG_Search_ConcurrentCalls_CompletesSuccessfully()
    {
        var sensitivityRegistry = new DataSensitivityRegistry();
        var store = new InMemoryVectorStore(sensitivityRegistry);
        var embedding = new TokenEmbeddingGenerator();
        var rag = new RAGService(store, embedding);
        await rag.IndexAsync("a", "Alpha content", "Public", default);
        await rag.IndexAsync("b", "Beta content", "Public", default);

        var tasks = Enumerable.Range(0, 30)
            .Select(_ => rag.SearchAsync("content", 5, 0.0, "Public", default))
            .ToList();
        var results = await Task.WhenAll(tasks);
        results.Should().HaveCount(30);
        results.All(r => r.Count <= 5).Should().BeTrue();
    }
}
