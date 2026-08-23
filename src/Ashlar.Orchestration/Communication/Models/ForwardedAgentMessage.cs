using System.Text.Json;

namespace Ashlar.Orchestration.Communication.Models;

/// <summary>
/// Agent message forwarded from the network bus.
/// Used by AgentBusNetworkBridge when delivering network events to the local agent bus;
/// subscribers can handle it like any AgentMessage. The bridge does not re-broadcast these.
/// </summary>
public sealed record ForwardedAgentMessage : AgentMessage
{
    /// <summary>
    /// Node ID that originally sent this message on the network.
    /// </summary>
    public string? SourceNodeId { get; init; }
}
