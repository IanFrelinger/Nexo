namespace Ashlar.API.Middleware.Ingress;

/// <summary>Ingress endpoint policies.</summary>
public static class IngressEndpointPolicies
{
    /// <summary>Returns whether  capability disabled.</summary>
    /// <param name="options">Options.</param>
    /// <param name="capability">Capability.</param>
    public static bool IsCapabilityDisabled(AshlarMiddlewareIngressOptions options, string capability) =>
        options.DisabledCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns whether a tenant allowlist permits the given ingress capability.</summary>
    public static bool TenantAllowsCapability(AshlarMiddlewareIngressOptions options, string? tenantId, string capability)
    {
        if (options.TenantCapabilityAllowlists.Count == 0 || string.IsNullOrWhiteSpace(tenantId))
            return true;

        if (!options.TenantCapabilityAllowlists.TryGetValue(tenantId.Trim(), out var allow))
            return true;

        return allow.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }
}
