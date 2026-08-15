namespace Nexo.API.Security;

/// <summary>
/// Auth-relevant knobs for the agent-protocol ingress surfaces (MCP + A2A), bound from the same
/// configuration the protocol adapters use so one key drives both mapping and auth behavior.
/// Kept as a separate small options class so <see cref="NexoApiKeyAuthMiddleware"/> stays
/// decoupled from the protocol adapter packages.
/// </summary>
public sealed class NexoProtocolIngressOptions
{
    /// <summary>
    /// Mirror of <c>Nexo:A2A:Server:AllowAnonymousAgentCard</c>: when true, agent-card discovery
    /// endpoints (root and per-agent <c>/.well-known/agent-card.json</c>) are exempt from the
    /// built-in auth modes. Default false — cards are credentialed like everything else.
    /// </summary>
    public bool AllowAnonymousAgentCard { get; set; }
}
