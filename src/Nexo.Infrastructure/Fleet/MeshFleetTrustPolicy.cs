using Nexo.Core.Application.Fleet.Models;

namespace Nexo.Infrastructure.Fleet;

internal static class MeshFleetTrustPolicy
{
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

    public static bool IsEligible(MeshFleetTrustTier tier, string normalizedPolicy) =>
        normalizedPolicy switch
        {
            "trusted-only" or "allowlist" => tier == MeshFleetTrustTier.Trusted,
            "trusted-preferred" => tier != MeshFleetTrustTier.Untrusted,
            _ => true
        };
}
