namespace Nexo.Core.Application.Networking.Models;

/// <summary>
/// Standard network event type constants for cross-node communication.
/// </summary>
public static class NetworkEventTypes
{
    public const string BrickCreated = "brick.created";
    public const string BrickRemoved = "brick.removed";
    public const string BrickUsageHot = "brick.usage.hot";
    public const string CatalogChanged = "catalog.changed";
    public const string AgentOutput = "agent.output";
    public const string AgentState = "agent.state";
    /// <summary>Carries a serialized agent message (MessageId, FromAgentId, ToAgentId, MessageType, Timestamp, Payload).</summary>
    public const string AgentMessage = "agent.message";
    public const string MemoryIndexed = "memory.indexed";
    public const string NodeJoined = "node.joined";
    public const string NodeLeaving = "node.leaving";
    public const string NodeHeartbeat = "node.heartbeat";
}
