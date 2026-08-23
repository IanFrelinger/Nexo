namespace Ashlar.BackgroundAgents.RAG;

/// <summary>
/// Helpers for vector similarity (e.g. cosine similarity).
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Cosine similarity between two vectors (0.0 to 1.0 when normalized).
    /// Returns 0 if either vector is zero.
    /// </summary>
    public static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        if (denom == 0)
            return 0;
        var sim = dot / denom;
        return Math.Clamp(sim, -1.0, 1.0);
    }

    /// <summary>
    /// Normalize vector in place to unit length.
    /// </summary>
    public static void NormalizeInPlace(Span<float> v)
    {
        double norm = 0;
        foreach (var x in v)
            norm += x * x;
        norm = Math.Sqrt(norm);
        if (norm == 0) return;
        for (var i = 0; i < v.Length; i++)
            v[i] = (float)(v[i] / norm);
    }
}
