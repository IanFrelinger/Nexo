namespace Ashlar.Commercial.Fleet.Infrastructure.Communication;

/// <summary>
/// Options for the bridge between IAgentBus and INetworkBus.
/// </summary>
public sealed class AgentBusNetworkBridgeOptions
{
    /// <summary>
    /// Configuration section name. Default: "AgentBusNetworkBridge".
    /// </summary>
    public const string SectionName = "AgentBusNetworkBridge";

    /// <summary>
    /// If true, agent messages published locally are broadcast to the network. Default: true.
    /// </summary>
    public bool ForwardAgentMessagesToNetwork { get; set; } = true;

    /// <summary>
    /// Message types to forward to the network (null or empty = all). E.g. ["OutputEmitted", "AgentStateChanged"].
    /// </summary>
    public IReadOnlyList<string>? ForwardMessageTypes { get; set; }

    /// <summary>
    /// Maximum hops for agent-message network events. Default: 3.
    /// </summary>
    public int MaxHops { get; set; } = 3;
}
