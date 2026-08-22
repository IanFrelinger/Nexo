using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Ashlar.AI.Pipeline.Embeddings;

/// <summary>
/// Deterministic local embedding generator (token-hash bag-of-words), MEAI-shaped.
/// Does not collide with Ashlar.BackgroundAgents.RAG.IEmbeddingGenerator.
/// </summary>
public sealed class TokenHashEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly int _dimensions;

    /// <summary>Creates a generator with the given dimensionality (default 64).</summary>
    public TokenHashEmbeddingGenerator(int dimensions = ChunkRecordDimensions.Default)
    {
        if (dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions));
        }

        _dimensions = dimensions;
    }

    /// <inheritdoc />
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = new List<Embedding<float>>();
        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(new Embedding<float>(Embed(value ?? string.Empty)));
        }

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list));
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private float[] Embed(string text)
    {
        var vector = new float[_dimensions];
        var tokens = text.Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            var hash = token.ToLowerInvariant().GetHashCode();
            var index = Math.Abs(hash) % _dimensions;
            vector[index] += 1f;
        }

        // L2 normalize
        double sumSq = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            sumSq += vector[i] * vector[i];
        }

        if (sumSq > double.Epsilon)
        {
            var norm = (float)Math.Sqrt(sumSq);
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }
}

/// <summary>Shared embedding dimension constant to avoid circular refs with Rag namespace.</summary>
internal static class ChunkRecordDimensions
{
    public const int Default = 64;
}
