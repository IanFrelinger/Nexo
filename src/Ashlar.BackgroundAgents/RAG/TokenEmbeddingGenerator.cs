using System.Collections.Concurrent;
using System.Text;
using Ashlar.Core.Domain;

namespace Ashlar.BackgroundAgents.RAG;

/// <summary>
/// Simple token-based embedding generator (fallback when no external embedding API).
/// Produces deterministic vectors from word tokens for approximate similarity search.
/// </summary>
public sealed class TokenEmbeddingGenerator : IEmbeddingGenerator
{
    private const int DefaultDimension = AshlarDefaults.EmbeddingDefaultDimension;
    private readonly int _dimension;
    private readonly ConcurrentDictionary<string, float[]> _tokenCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Dimension of produced vectors.
    /// </summary>
    public int Dimension => _dimension;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenEmbeddingGenerator"/> class.
    /// </summary>
    /// <param name="dimension">Vector dimension (default 64).</param>
    public TokenEmbeddingGenerator(int dimension = DefaultDimension)
    {
        if (dimension <= 0)
            throw new ArgumentOutOfRangeException(nameof(dimension));
        _dimension = dimension;
    }

    /// <inheritdoc />
    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            return Task.FromResult(new float[_dimension]);

        var tokens = Tokenize(text);
        var vector = new float[_dimension];

        foreach (var token in tokens)
        {
            var tokenVector = GetOrCreateTokenVector(token);
            for (var i = 0; i < _dimension; i++)
                vector[i] += tokenVector[i];
        }

        VectorMath.NormalizeInPlace(vector);
        return Task.FromResult(vector);
    }

    private float[] GetOrCreateTokenVector(string token)
    {
        return _tokenCache.GetOrAdd(token, t =>
        {
            var v = new float[_dimension];
            var hash = (uint)StringComparer.OrdinalIgnoreCase.GetHashCode(t);
            for (var i = 0; i < _dimension; i++)
            {
                hash = (hash * 31 + (uint)i) ^ (hash >> 13);
                v[i] = (hash % 1000) / 1000f - 0.5f;
            }
            VectorMath.NormalizeInPlace(v);
            return v;
        });
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '\'')
                sb.Append(char.ToLowerInvariant(c));
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }
}
