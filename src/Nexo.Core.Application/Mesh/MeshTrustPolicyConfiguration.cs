namespace Nexo.Core.Application.Mesh;

/// <summary>
/// Normalizes peer trust policy strings and resolves the effective policy for mesh CLI/discovery.
/// </summary>
public static class MeshTrustPolicyConfiguration
{
    /// <summary>
    /// Normalizes a policy token to trusted-only, trusted-preferred, or any.
    /// Unknown values default to trusted-preferred (fail-closed for routing).
    /// </summary>
    public static string NormalizePolicy(string? policy)
    {
        var normalized = (policy ?? "trusted-preferred").Trim().ToLowerInvariant();
        return normalized switch
        {
            "trusted-only" => "trusted-only",
            "trusted-preferred" => "trusted-preferred",
            "any" => "any",
            _ => "trusted-preferred"
        };
    }

    /// <summary>
    /// Policy used by mesh discovery: NEXO_MESH_TRUST_POLICY, else NEXO_PEER_TRUST_POLICY, else any (show all tiers).
    /// </summary>
    public static string ResolveDiscoveryPolicy()
    {
        var mesh = Environment.GetEnvironmentVariable("NEXO_MESH_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(mesh))
            return NormalizePolicy(mesh);
        var peer = Environment.GetEnvironmentVariable("NEXO_PEER_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(peer))
            return NormalizePolicy(peer);
        return "any";
    }

    /// <summary>
    /// Policy for mesh capability requests: NEXO_MESH_TRUST_POLICY, else NEXO_PEER_TRUST_POLICY, else trusted-preferred.
    /// </summary>
    public static string ResolveCapabilityRequestPolicy()
    {
        var mesh = Environment.GetEnvironmentVariable("NEXO_MESH_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(mesh))
            return NormalizePolicy(mesh);
        var peer = Environment.GetEnvironmentVariable("NEXO_PEER_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(peer))
            return NormalizePolicy(peer);
        return "trusted-preferred";
    }
}
