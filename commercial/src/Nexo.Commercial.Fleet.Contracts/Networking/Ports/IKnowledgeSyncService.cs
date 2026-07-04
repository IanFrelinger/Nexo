using Nexo.Commercial.Fleet.Contracts.Networking.Models;

namespace Nexo.Commercial.Fleet.Contracts.Networking.Ports;
/// <summary>
/// Cross-node knowledge/memory sync: push chunks to peers and pull from a peer.
/// </summary>
public interface IKnowledgeSyncService
{
    /// <summary>Push a chunk to all configured peers (and optionally store locally).</summary>
    Task PushAsync(KnowledgeChunk chunk, CancellationToken cancellationToken = default);

    /// <summary>Pull chunks from a specific peer node. Returns chunks that peer has for the given optional filter.</summary>
    Task<IReadOnlyList<KnowledgeChunk>> PullAsync(string nodeId, string? contentType = null, int maxCount = 100, CancellationToken cancellationToken = default);

    /// <summary>Sync status: this node id and last sync time per peer (if available).</summary>
    Task<KnowledgeSyncStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
