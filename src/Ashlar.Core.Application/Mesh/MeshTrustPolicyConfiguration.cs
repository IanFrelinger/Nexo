namespace Ashlar.Core.Application.Mesh;

/// <summary>
/// Normalizes peer trust policy strings and resolves the effective policy for mesh CLI/discovery.
/// </summary>
public static class MeshTrustPolicyConfiguration
{
    /// <summary>
    /// Normalizes a policy token to trusted-only, trusted-preferred, or any.
    /// Unknown values default to trusted-preferred (fail-closed for routing).
    /// </summary>
    /// <param name="policy">Raw policy string from config or environment.</param>
    /// <returns>Normalized policy token.</returns>
    public static string NormalizePolicy(string? policy)
    {
        var normalized = (policy ?? "trusted-preferred").Trim().ToLowerInvariant();
        return normalized switch
        {
            "trusted-only" => "trusted-only",
            "trusted-preferred" => "trusted-preferred",
            "any" => "any",
            "allowlist" => "allowlist",
            _ => "trusted-preferred"
        };
    }

    /// <summary>
    /// Policy used by mesh discovery: ASHLAR_MESH_TRUST_POLICY, else ASHLAR_PEER_TRUST_POLICY, else any (show all tiers).
    /// </summary>
    /// <returns>Effective discovery trust policy.</returns>
    public static string ResolveDiscoveryPolicy()
    {
        var mesh = Environment.GetEnvironmentVariable("ASHLAR_MESH_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(mesh))
            return NormalizePolicy(mesh);
        var peer = Environment.GetEnvironmentVariable("ASHLAR_PEER_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(peer))
            return NormalizePolicy(peer);
        return "any";
    }

    /// <summary>
    /// Policy for mesh capability requests: ASHLAR_MESH_TRUST_POLICY, else ASHLAR_PEER_TRUST_POLICY, else trusted-preferred.
    /// </summary>
    /// <returns>Effective capability request trust policy.</returns>
    public static string ResolveCapabilityRequestPolicy()
    {
        var mesh = Environment.GetEnvironmentVariable("ASHLAR_MESH_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(mesh))
            return NormalizePolicy(mesh);
        var peer = Environment.GetEnvironmentVariable("ASHLAR_PEER_TRUST_POLICY");
        if (!string.IsNullOrWhiteSpace(peer))
            return NormalizePolicy(peer);
        return "trusted-preferred";
    }
}
