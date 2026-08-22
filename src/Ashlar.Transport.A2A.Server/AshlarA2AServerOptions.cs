namespace Ashlar.Transport.A2A.Server;

/// <summary>
/// Options for exposing Ashlar agents over the A2A protocol.
///
/// Fail-closed by design: <see cref="Enabled"/> defaults to <see langword="false"/> and the
/// exposure allowlist defaults to empty. v1 is synchronous — cards advertise
/// <c>streaming=false, pushNotifications=false</c> and every <c>message/send</c> completes with
/// a terminal task within the request (Ashlar has no durable task machinery yet; task state lives
/// in per-agent in-memory stores good for <c>tasks/get</c> within a process lifetime).
/// </summary>
public sealed class AshlarA2AServerOptions
{
    /// <summary>Default configuration section (<c>Ashlar:A2A:Server</c>).</summary>
    public const string SectionPath = "Ashlar:A2A:Server";

    /// <summary>Master switch; defaults to <see langword="false"/> (no endpoints are mapped).</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Explicit allowlist of Ashlar agent ids to expose. Empty exposes zero agents unless
    /// <see cref="ExposeByCoordinationProtocol"/> is opted into.
    /// </summary>
    public IList<string> ExposedAgentIds { get; } = new List<string>();

    /// <summary>
    /// Opt-in: additionally expose agents whose <c>AgentCard.CoordinationProtocols</c> contains
    /// <c>"a2a"</c> — the domain model's own marker for cross-agent coordination support.
    /// </summary>
    public bool ExposeByCoordinationProtocol { get; set; }

    /// <summary>
    /// Public absolute base URL of this host (e.g. <c>https://ashlar.example.com</c>); used to
    /// build the interface URLs embedded in agent cards. Required when enabled.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Agent whose card is served at the root <c>/.well-known/agent-card.json</c> discovery
    /// path. Optional when exactly one agent is exposed (it becomes the primary); required when
    /// several are. Per-agent cards are always served under each agent's own endpoint prefix.
    /// </summary>
    public string? PrimaryAgentId { get; set; }

    /// <summary>
    /// Whether the agent-card discovery endpoints may be served without authentication.
    /// Defaults to <see langword="false"/> (cards inherit host auth like everything else);
    /// hosts consult this when attaching auth metadata to the card endpoints. Turning it on is
    /// the documented opt-in for public-ecosystem discovery and slightly leaks agent inventory.
    /// </summary>
    public bool AllowAnonymousAgentCard { get; set; }

    /// <summary>
    /// Fallback per-invocation execution budget when the agent's own constraints don't specify
    /// one. Bounded because an internet-facing endpoint must never hold requests open forever.
    /// </summary>
    public TimeSpan DefaultExecutionTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
