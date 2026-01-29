using FluentAssertions;
using Nexo.BackgroundAgents.RAG;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RAG;

public class SqliteVectorStoreTests
{
    [Fact]
    public async Task IndexAsync_And_SearchAsync_ReturnsMatchingDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rag_test_{Guid.NewGuid():N}.db");
        await using var store = new SqliteVectorStore(path);
        var gen = new TokenEmbeddingGenerator(32);
        var emb = await gen.GenerateAsync("hello sqlite", default);
        await store.IndexAsync("doc1", "hello sqlite", emb, null, default);

        var queryEmb = await gen.GenerateAsync("hello sqlite", default);
        var results = await store.SearchAsync(queryEmb, 5, 0.0, null, default);

        results.Should().HaveCount(1);
        results[0].Id.Should().Be("doc1");
        results[0].Text.Should().Be("hello sqlite");
    }

    [Fact]
    public async Task RemoveAsync_RemovesDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rag_test_{Guid.NewGuid():N}.db");
        await using var store = new SqliteVectorStore(path);
        var gen = new TokenEmbeddingGenerator(32);
        await store.IndexAsync("doc1", "text", await gen.GenerateAsync("text", default), null, default);
        await store.RemoveAsync("doc1", default);

        var results = await store.SearchAsync(await gen.GenerateAsync("text", default), 5, 0.0, null, default);
        results.Should().BeEmpty();
    }
}
