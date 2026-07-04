namespace Nexo.API.Middleware.Ingress;

/// <summary>Ingress endpoint policies.</summary>
public static class IngressEndpointPolicies
{
    /// <summary>Returns whether  capability disabled.</summary>
    /// <param name="options">Options.</param>
    /// <param name="capability">Capability.</param>
    public static bool IsCapabilityDisabled(NexoMiddlewareIngressOptions options, string capability) =>
        options.DisabledCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

    public static bool TenantAllowsCapability(NexoMiddlewareIngressOptions options, string? tenantId, string capability)
    {
        if (options.TenantCapabilityAllowlists.Count == 0 || string.IsNullOrWhiteSpace(tenantId))
            return true;

        if (!options.TenantCapabilityAllowlists.TryGetValue(tenantId.Trim(), out var allow))
            return true;

        return allow.Contains(capability, StringComparer.OrdinalIgnoreCase);
    }
}
