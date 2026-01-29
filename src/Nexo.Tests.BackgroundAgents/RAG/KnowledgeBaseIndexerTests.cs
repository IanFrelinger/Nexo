using System.IO;
using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.RAG;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RAG;

public class KnowledgeBaseIndexerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public KnowledgeBaseIndexerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kb_index_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "doc.txt");
        File.WriteAllText(_tempFile, "Nexo background agents support RAG.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task IndexDocumentsAsync_IndexesFile_AndSearchFindsIt()
    {
        var store = new InMemoryVectorStore();
        var gen = new TokenEmbeddingGenerator(32);
        var service = new RAGService(store, gen);
        var indexer = new KnowledgeBaseIndexer(service);

        var count = await indexer.IndexDocumentsAsync(new[] { _tempFile }, null, default);

        count.Should().Be(1);
        var results = await service.SearchAsync("RAG", 5, 0.0, null, default);
        results.Should().HaveCount(1);
        results[0].Text.Should().Contain("RAG");
    }

    [Fact]
    public async Task IndexDocumentsAsync_WithDirectory_IndexesMatchingFiles()
    {
        var other = Path.Combine(_tempDir, "other.md");
        File.WriteAllText(other, "Markdown content");
        var store = new InMemoryVectorStore();
        var gen = new TokenEmbeddingGenerator(32);
        var service = new RAGService(store, gen);
        var indexer = new KnowledgeBaseIndexer(service);

        var count = await indexer.IndexDocumentsAsync(new[] { _tempDir }, "Internal", default);

        count.Should().Be(2);
    }
}
