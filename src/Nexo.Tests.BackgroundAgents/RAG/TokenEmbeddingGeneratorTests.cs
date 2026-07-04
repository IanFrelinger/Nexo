using FluentAssertions;
using Nexo.BackgroundAgents.RAG;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.RAG;

/// <summary>Tests for token embedding generator.</summary>
public class TokenEmbeddingGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsVectorOfCorrectDimension()
    {
        var gen = new TokenEmbeddingGenerator(64);
        var v = await gen.GenerateAsync("hello world", default);
        v.Should().HaveCount(64);
    }

    [Fact]
    public async Task GenerateAsync_SameText_ReturnsSameVector()
    {
        var gen = new TokenEmbeddingGenerator(32);
        var a = await gen.GenerateAsync("deterministic", default);
        var b = await gen.GenerateAsync("deterministic", default);
        a.Should().Equal(b);
    }

    [Fact]
    public async Task GenerateAsync_EmptyString_ReturnsZeroVector()
    {
        var gen = new TokenEmbeddingGenerator(32);
        var v = await gen.GenerateAsync("", default);
        v.Should().HaveCount(32);
        v.Should().OnlyContain(x => x == 0f);
    }

    [Fact]
    public async Task GenerateAsync_SimilarText_HigherSimilarityThanDifferent()
    {
        var gen = new TokenEmbeddingGenerator(64);
        var similar = await gen.GenerateAsync("machine learning", default);
        var same = await gen.GenerateAsync("machine learning", default);
        var different = await gen.GenerateAsync("potato salad", default);
        var simSame = VectorMath.CosineSimilarity(similar.AsSpan(), same.AsSpan());
        var simDiff = VectorMath.CosineSimilarity(similar.AsSpan(), different.AsSpan());
        simSame.Should().BeGreaterThan(simDiff);
    }
}
