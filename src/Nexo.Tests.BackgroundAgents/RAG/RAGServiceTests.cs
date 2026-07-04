using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.RAG;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RAG;

/// <summary>Tests for rag service.</summary>
public class RAGServiceTests
{
    [Fact]
    public async Task IndexAsync_And_SearchAsync_ReturnsResults()
    {
        var store = new InMemoryVectorStore();
        var gen = new TokenEmbeddingGenerator(32);
        var service = new RAGService(store, gen);

        await service.IndexAsync("id1", "Nexo is a game framework", null, default);
        var results = await service.SearchAsync("Nexo framework", 5, 0.0, null, default);

        results.Should().HaveCount(1);
        results[0].Id.Should().Be("id1");
        results[0].Text.Should().Contain("Nexo");
    }

    [Fact]
    public async Task ClearAsync_RemovesAll()
    {
        var store = new InMemoryVectorStore();
        var gen = new TokenEmbeddingGenerator(32);
        var service = new RAGService(store, gen);
        await service.IndexAsync("id1", "content", null, default);
        await service.ClearAsync(default);

        var results = await service.SearchAsync("content", 5, 0.0, null, default);
        results.Should().BeEmpty();
    }
}
