namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// Implementation of IRAGService using a vector store and embedding generator.
/// </summary>
public sealed class RAGService : IRAGService
{
    private readonly IVectorStore _store;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RAGService"/> class.
    /// </summary>
    /// <param name="store">Vector store.</param>
    /// <param name="embeddingGenerator">Embedding generator.</param>
    public RAGService(IVectorStore store, IEmbeddingGenerator embeddingGenerator)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int maxResults,
        double minScore,
        string? maxSensitivityLevelName,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(query ?? string.Empty, cancellationToken).ConfigureAwait(false);
        return await _store.SearchAsync(embedding, maxResults, minScore, maxSensitivityLevelName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task IndexAsync(
        string id,
        string text,
        string? sensitivityLevelName,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(text ?? string.Empty, cancellationToken).ConfigureAwait(false);
        await _store.IndexAsync(id, text ?? string.Empty, embedding, sensitivityLevelName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        => _store.RemoveAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => _store.ClearAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
        => _store.GetDocumentCountAsync(cancellationToken);
}
