using Nexo.Commercial.Fleet.Contracts.Models;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>Mesh fleet trust policy.</summary>
internal static class MeshFleetTrustPolicy
{
    /// <summary>Normalize policy operation.</summary>
    public static string NormalizePolicy(string? policy)
    {
        var p = (policy ?? "any").Trim().ToLowerInvariant();
        return p switch
        {
            "trusted-only" or "trusted_only" => "trusted-only",
            "trusted-preferred" or "trusted_preferred" => "trusted-preferred",
            "allowlist" => "allowlist",
            _ => "any"
        };
    }

    /// <summary>Returns whether  eligible.</summary>
    /// <param name="tier">Tier.</param>
    /// <param name="normalizedPolicy">Normalized policy.</param>
    public static bool IsEligible(MeshFleetTrustTier tier, string normalizedPolicy) =>
        normalizedPolicy switch
        {
            "trusted-only" or "allowlist" => tier == MeshFleetTrustTier.Trusted,
            "trusted-preferred" => tier != MeshFleetTrustTier.Untrusted,
            _ => true
        };
}
