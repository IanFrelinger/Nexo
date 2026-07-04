namespace Nexo.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Standard network event type constants for cross-node communication.
/// </summary>
public static class NetworkEventTypes
{
    /// <summary>Emitted when a new brick is created on a node.</summary>
    public const string BrickCreated = "brick.created";
    /// <summary>Emitted when a brick is removed from a node.</summary>
    public const string BrickRemoved = "brick.removed";
    /// <summary>Signals that a brick crossed hot-usage thresholds.</summary>
    public const string BrickUsageHot = "brick.usage.hot";
    /// <summary>Emitted when the shared brick catalog changes.</summary>
    public const string CatalogChanged = "catalog.changed";
    /// <summary>Carries agent output text or structured results.</summary>
    public const string AgentOutput = "agent.output";
    /// <summary>Reports agent lifecycle or runtime state changes.</summary>
    public const string AgentState = "agent.state";
    /// <summary>Carries a serialized agent message (MessageId, FromAgentId, ToAgentId, MessageType, Timestamp, Payload).</summary>
    public const string AgentMessage = "agent.message";
    /// <summary>Emitted when knowledge memory is indexed on a node.</summary>
    public const string MemoryIndexed = "memory.indexed";
    /// <summary>Announces that a mesh node joined the fleet.</summary>
    public const string NodeJoined = "node.joined";
    /// <summary>Announces that a mesh node is leaving the fleet.</summary>
    public const string NodeLeaving = "node.leaving";
    /// <summary>Periodic liveness signal from a mesh node.</summary>
    public const string NodeHeartbeat = "node.heartbeat";
}
