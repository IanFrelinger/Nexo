using Nexo.Core.Application.Networking.Models;

namespace Nexo.Core.Application.Networking.Ports;

/// <summary>
/// Local store for knowledge chunks received from the network (used by the sync API).
/// </summary>
public interface IKnowledgeChunkStore
{
    /// <summary>Add a chunk (e.g. received from a peer).</summary>
    Task AddAsync(KnowledgeChunk chunk, CancellationToken cancellationToken = default);

    /// <summary>Get chunks optionally filtered by content type, newest first, limited by count.</summary>
    Task<IReadOnlyList<KnowledgeChunk>> GetAsync(string? contentType = null, int maxCount = 100, CancellationToken cancellationToken = default);

    /// <summary>Remove a chunk by id.</summary>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
