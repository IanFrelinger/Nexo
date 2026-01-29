namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// RAG (Retrieval Augmented Generation) service: indexes text with embeddings and runs sensitivity-aware similarity search.
/// </summary>
public interface IRAGService
{
    /// <summary>
    /// Search for documents similar to the query text.
    /// </summary>
    /// <param name="query">Query text.</param>
    /// <param name="maxResults">Maximum number of results.</param>
    /// <param name="minScore">Minimum similarity score (0.0-1.0).</param>
    /// <param name="maxSensitivityLevelName">If set, only return documents at or below this sensitivity level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results ordered by score descending.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int maxResults,
        double minScore,
        string? maxSensitivityLevelName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Index a document (or chunk) with optional sensitivity level.
    /// </summary>
    /// <param name="id">Document id.</param>
    /// <param name="text">Document text.</param>
    /// <param name="sensitivityLevelName">Optional sensitivity level name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexAsync(
        string id,
        string text,
        string? sensitivityLevelName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a document by id.
    /// </summary>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all documents.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the number of documents in the RAG store.
    /// </summary>
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);
}

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
