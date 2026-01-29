namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// Generates embedding vectors for text for use in vector search.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Dimension of vectors produced by this generator.
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Embedding vector of length Dimension.</returns>
    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);
}
