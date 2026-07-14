using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Nexo.AI.Pipeline.Rag;

/// <summary>
/// In-process <see cref="VectorStore"/> for local-first RAG (GA Abstractions only; no preview connectors).
/// Keeps the legacy Nexo store untouched until Phase 6 cutover.
/// </summary>
public sealed class InProcessVectorStore : VectorStore
{
    private readonly ConcurrentDictionary<string, object> _collections =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(
        string name,
        VectorStoreCollectionDefinition? definition = null)
    {
        if (typeof(TKey) != typeof(string) || typeof(TRecord) != typeof(ChunkRecord))
        {
            throw new NotSupportedException("InProcessVectorStore currently supports VectorStoreCollection<string, ChunkRecord> only.");
        }

        var collection = (VectorStoreCollection<TKey, TRecord>)_collections.GetOrAdd(
            name,
            _ => new InProcessChunkCollection(name));
        return collection;
    }

    /// <inheritdoc />
    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(
        string name,
        VectorStoreCollectionDefinition definition) =>
        throw new NotSupportedException("Dynamic collections are not supported by InProcessVectorStore.");

    /// <inheritdoc />
    public override IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken = default) =>
        _collections.Keys.ToAsyncEnumerable();

    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_collections.ContainsKey(name));

    /// <inheritdoc />
    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = default)
    {
        _collections.TryRemove(name, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;
}

/// <summary>
/// In-memory cosine-similarity collection for <see cref="ChunkRecord"/>.
/// </summary>
public sealed class InProcessChunkCollection : VectorStoreCollection<string, ChunkRecord>
{
    private readonly string _name;
    private readonly ConcurrentDictionary<string, ChunkRecord> _records =
        new(StringComparer.Ordinal);

    /// <summary>Creates a named chunk collection.</summary>
    public InProcessChunkCollection(string name) =>
        _name = string.IsNullOrWhiteSpace(name) ? "chunks" : name;

    /// <inheritdoc />
    public override string Name => _name;

    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    /// <inheritdoc />
    public override Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public override Task EnsureCollectionDeletedAsync(CancellationToken cancellationToken = default)
    {
        _records.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<ChunkRecord?> GetAsync(
        string key,
        RecordRetrievalOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(key, out var record);
        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChunkRecord> GetAsync(
        Expression<Func<ChunkRecord, bool>> filter,
        int top,
        FilteredRecordRetrievalOptions<ChunkRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var predicate = filter.Compile();
        var taken = 0;
        foreach (var record in _records.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!predicate(record))
            {
                continue;
            }

            yield return record;
            if (++taken >= top)
            {
                yield break;
            }

            await Task.Yield();
        }
    }

    /// <inheritdoc />
    public override Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _records.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task UpsertAsync(ChunkRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (string.IsNullOrWhiteSpace(record.Key))
        {
            throw new ArgumentException("ChunkRecord.Key is required.", nameof(record));
        }

        record.UpdatedAt = DateTimeOffset.UtcNow;
        _records[record.Key] = Clone(record);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task UpsertAsync(
        IEnumerable<ChunkRecord> records,
        CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            await UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<VectorSearchResult<ChunkRecord>> SearchAsync<TInput>(
        TInput searchValue,
        int top,
        VectorSearchOptions<ChunkRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = ToVector(searchValue);
        Func<ChunkRecord, bool>? filter = options?.Filter?.Compile();
        var skip = options?.Skip ?? 0;
        var threshold = options?.ScoreThreshold;

        var scored = new List<(ChunkRecord Record, double Score)>();
        foreach (var record in _records.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (filter is not null && !filter(record))
            {
                continue;
            }

            var score = CosineSimilarity(query, record.Embedding.ToArray());
            if (threshold is not null && score < threshold.Value)
            {
                continue;
            }

            scored.Add((record, score));
        }

        foreach (var item in scored.OrderByDescending(s => s.Score).Skip(skip).Take(top))
        {
            yield return new VectorSearchResult<ChunkRecord>(Clone(item.Record), item.Score);
            await Task.Yield();
        }
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    /// <summary>Number of records currently stored.</summary>
    public int Count => _records.Count;

    private static float[] ToVector<TInput>(TInput searchValue)
    {
        return searchValue switch
        {
            ReadOnlyMemory<float> rom => rom.ToArray(),
            float[] arr => arr,
            Embedding<float> emb => emb.Vector.ToArray(),
            _ => throw new NotSupportedException($"Unsupported search input type: {typeof(TInput).Name}"),
        };
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        var len = Math.Min(a.Length, b.Length);
        if (len == 0)
        {
            return 0;
        }

        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na <= double.Epsilon || nb <= double.Epsilon)
        {
            return 0;
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static ChunkRecord Clone(ChunkRecord r) => new()
    {
        Key = r.Key,
        SourceUri = r.SourceUri,
        Text = r.Text,
        TrustTier = r.TrustTier,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Embedding = r.Embedding.ToArray(),
    };
}

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
