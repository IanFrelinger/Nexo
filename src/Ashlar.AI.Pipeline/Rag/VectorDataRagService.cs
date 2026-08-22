using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Ashlar.AI.Pipeline.Embeddings;
using Ashlar.AI.Pipeline.Governance;

namespace Ashlar.AI.Pipeline.Rag;

/// <summary>
/// Indexes and searches <see cref="ChunkRecord"/> via VectorData + governed embeddings.
/// </summary>
public sealed class VectorDataRagService
{
    public const string DefaultCollectionName = "ashlar-chunks";

    private readonly VectorStoreCollection<string, ChunkRecord> _collection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IChatInvocationAuditor _auditor;

    /// <summary>Creates a RAG façade over a VectorData collection.</summary>
    public VectorDataRagService(
        VectorStoreCollection<string, ChunkRecord> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IChatInvocationAuditor auditor)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    }

    /// <summary>Indexes a chunk (embed + upsert).</summary>
    public async Task IndexAsync(
        string key,
        string text,
        string? sourceUri = null,
        string trustTier = "Public",
        CancellationToken cancellationToken = default)
    {
        var embedding = await EmbedAsync(text, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        await _collection.UpsertAsync(new ChunkRecord
        {
            Key = key,
            Text = text,
            SourceUri = sourceUri ?? string.Empty,
            TrustTier = trustTier,
            CreatedAt = now,
            UpdatedAt = now,
            Embedding = embedding,
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for similar chunks, excluding records above <paramref name="callerMaxTrustTier"/>.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult<ChunkRecord>>> SearchAsync(
        string query,
        string callerMaxTrustTier,
        int top = 5,
        double? minScore = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await EmbedAsync(query, cancellationToken).ConfigureAwait(false);
        var maxRank = TrustTierOrder.Rank(callerMaxTrustTier);
        var options = new VectorSearchOptions<ChunkRecord>
        {
            Filter = r => TrustTierOrder.Rank(r.TrustTier) <= maxRank,
            ScoreThreshold = minScore,
        };

        var results = new List<VectorSearchResult<ChunkRecord>>();
        await foreach (var hit in _collection.SearchAsync(embedding, top, options, cancellationToken)
            .ConfigureAwait(false))
        {
            results.Add(hit);
        }

        _auditor.Record(new ChatInvocationAuditRecord
        {
            TargetKey = "rag:search",
            Outcome = "success",
            PolicyDecisions = new[]
            {
                "event=retrieval",
                $"results={results.Count}",
                $"caller_tier={callerMaxTrustTier}",
            },
        });

        return results;
    }

    /// <summary>Re-indexes a batch of existing chunks into the VectorData store.</summary>
    public async Task<int> ReindexAsync(
        IEnumerable<(string Key, string Text, string? SourceUri, string TrustTier)> chunks,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var chunk in chunks)
        {
            await IndexAsync(chunk.Key, chunk.Text, chunk.SourceUri, chunk.TrustTier, cancellationToken)
                .ConfigureAwait(false);
            count++;
        }

        _auditor.Record(new ChatInvocationAuditRecord
        {
            TargetKey = "rag:reindex",
            Outcome = "success",
            PolicyDecisions = new[] { "event=reindex", $"count={count}" },
        });
        return count;
    }

    /// <summary>Removes a chunk by key.</summary>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _collection.DeleteAsync(key, cancellationToken);

    /// <summary>Clears the default collection.</summary>
    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _collection.EnsureCollectionDeletedAsync(cancellationToken);

    /// <summary>Returns the number of indexed chunks when the store is in-process.</summary>
    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_collection is InProcessChunkCollection inProcess)
        {
            return Task.FromResult(inProcess.Count);
        }

        return Task.FromResult(-1);
    }

    private async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var generated = await _embeddings.GenerateAsync([text], cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return generated[0].Vector.ToArray();
    }
}
