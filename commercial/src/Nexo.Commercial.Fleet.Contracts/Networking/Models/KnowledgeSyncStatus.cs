namespace Nexo.Core.Application.Networking.Models;

/// <summary>
/// Status of knowledge sync for this node.
/// </summary>
public sealed record KnowledgeSyncStatus
{
    /// <summary>This node's identifier.</summary>
    public required string NodeId { get; init; }

    /// <summary>Peer node IDs that are configured for sync.</summary>
    public IReadOnlyList<string> PeerIds { get; init; } = [];

    /// <summary>Last successful sync time per peer (nodeId -> timestamp).</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> LastSyncByPeer { get; init; } = new Dictionary<string, DateTimeOffset>();
}
