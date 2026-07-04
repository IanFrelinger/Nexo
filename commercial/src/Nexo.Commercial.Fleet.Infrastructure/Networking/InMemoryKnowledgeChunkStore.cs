using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Commercial.Fleet.Contracts.Networking.Models;
using Nexo.Commercial.Fleet.Contracts.Networking.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// In-memory store for knowledge chunks received from the network.
/// </summary>
public sealed class InMemoryKnowledgeChunkStore : IKnowledgeChunkStore
{
    private readonly ILogger<InMemoryKnowledgeChunkStore>? _logger;
    private readonly ConcurrentDictionary<string, KnowledgeChunk> _chunks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _order = new();
    private readonly int _maxChunks;
    private readonly object _trimLock = new();

    public InMemoryKnowledgeChunkStore(ILogger<InMemoryKnowledgeChunkStore>? logger = null, int maxChunks = 10_000)
    {
        _logger = logger;
        _maxChunks = maxChunks;
    }

    /// <inheritdoc />
    public Task AddAsync(KnowledgeChunk chunk, CancellationToken cancellationToken = default)
    {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));
        if (string.IsNullOrEmpty(chunk.Id)) throw new ArgumentException("Chunk Id is required.", nameof(chunk));

        _chunks[chunk.Id] = chunk;
        _order.Enqueue(chunk.Id);
        TrimIfNeeded();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<KnowledgeChunk>> GetAsync(string? contentType = null, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        var list = _chunks.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(contentType))
            list = list.Where(c => string.Equals(c.ContentType, contentType, StringComparison.OrdinalIgnoreCase));
        var ordered = list.OrderByDescending(c => c.Timestamp).Take(maxCount).ToList();
        return Task.FromResult<IReadOnlyList<KnowledgeChunk>>(ordered);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _chunks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private void TrimIfNeeded()
    {
        if (_chunks.Count <= _maxChunks) return;
        lock (_trimLock)
        {
            while (_chunks.Count > _maxChunks && _order.TryDequeue(out var id))
                _chunks.TryRemove(id, out _);
        }
    }
}
