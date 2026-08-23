namespace Ashlar.Transport.A2A.Server;

/// <summary>
/// Port supplying the Ashlar agents that may be considered for A2A exposure.
///
/// Deliberately DTO-based with no reference to <c>Ashlar.Core.Domain</c>: the transport layering
/// rules keep server adapters off the domain model (mirroring the gRPC server), so the host
/// implements this over its <c>IAgentRegistry</c> and the adapter only ever sees plain data.
/// Allowlist filtering happens centrally in this project — the catalog returns candidates, not
/// decisions.
/// </summary>
public interface IAshlarA2AAgentCatalog
{
    /// <summary>All candidate agents (unfiltered; the adapter applies the exposure policy).</summary>
    IReadOnlyList<AshlarA2AAgentDescriptor> GetAgents();
}

/// <summary>Plain projection of a Ashlar agent for A2A purposes.</summary>
/// <param name="Id">Stable agent id (used in endpoint paths and invocation).</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Short description.</param>
/// <param name="Version">Agent definition version.</param>
/// <param name="Domain">Primary domain/specialty label (becomes a skill tag).</param>
/// <param name="Behaviors">Behavior names (become A2A skills).</param>
/// <param name="CoordinationProtocols">Declared coordination protocols (an entry of <c>"a2a"</c> opts into protocol-based exposure).</param>
/// <param name="MaxExecutionTime">Optional per-invocation budget from the agent's constraints.</param>
public sealed record AshlarA2AAgentDescriptor(
    string Id,
    string Name,
    string Description,
    string Version,
    string? Domain,
    IReadOnlyList<string> Behaviors,
    IReadOnlyList<string> CoordinationProtocols,
    TimeSpan? MaxExecutionTime);
