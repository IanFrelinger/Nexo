namespace Nexo.Transport.A2A.Server;

/// <summary>
/// Central exposure decision: an agent is exposed when it is explicitly allowlisted OR (opt-in)
/// declares the <c>a2a</c> coordination protocol. Lives in one place so no host can accidentally
/// widen the surface by re-implementing the filter.
/// </summary>
public static class NexoA2AExposurePolicy
{
    /// <summary>Coordination-protocol marker that opts an agent into exposure.</summary>
    public const string CoordinationProtocolMarker = "a2a";

    /// <summary>Applies the exposure policy to catalog candidates.</summary>
    public static IReadOnlyList<NexoA2AAgentDescriptor> FilterExposed(
        IReadOnlyList<NexoA2AAgentDescriptor> candidates,
        NexoA2AServerOptions options)
    {
        var allowlist = new HashSet<string>(options.ExposedAgentIds, StringComparer.OrdinalIgnoreCase);
        var exposed = new List<NexoA2AAgentDescriptor>();
        foreach (var candidate in candidates)
        {
            var allowlisted = allowlist.Contains(candidate.Id);
            var byProtocol = options.ExposeByCoordinationProtocol &&
                             candidate.CoordinationProtocols.Any(p =>
                                 string.Equals(p, CoordinationProtocolMarker, StringComparison.OrdinalIgnoreCase));
            if (allowlisted || byProtocol)
            {
                exposed.Add(candidate);
            }
        }

        return exposed;
    }

    /// <summary>
    /// Resolves the primary agent (whose card is served at the root well-known path).
    /// Throws on ambiguity rather than guessing — serving the wrong card to the ecosystem is a
    /// misconfiguration that must surface at startup.
    /// </summary>
    public static NexoA2AAgentDescriptor ResolvePrimary(
        IReadOnlyList<NexoA2AAgentDescriptor> exposed,
        NexoA2AServerOptions options)
    {
        if (exposed.Count == 0)
        {
            throw new InvalidOperationException(
                "A2A server is enabled but no agents are exposed. Allowlist agent ids in " +
                $"{NexoA2AServerOptions.SectionPath}:{nameof(NexoA2AServerOptions.ExposedAgentIds)} " +
                "or opt into coordination-protocol exposure.");
        }

        if (!string.IsNullOrWhiteSpace(options.PrimaryAgentId))
        {
            return exposed.FirstOrDefault(a => string.Equals(a.Id, options.PrimaryAgentId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"PrimaryAgentId '{options.PrimaryAgentId}' is not among the exposed agents.");
        }

        return exposed.Count == 1
            ? exposed[0]
            : throw new InvalidOperationException(
                $"{exposed.Count} agents are exposed; set {NexoA2AServerOptions.SectionPath}:" +
                $"{nameof(NexoA2AServerOptions.PrimaryAgentId)} to choose whose card serves the root " +
                "/.well-known/agent-card.json discovery path.");
    }
}
