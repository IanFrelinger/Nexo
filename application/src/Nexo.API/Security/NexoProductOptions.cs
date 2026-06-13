namespace Nexo.API.Security;

/// <summary>
/// Tenant identity defaults for API-scoped persistence (copilot history, onboarding checks).
/// </summary>
public sealed class NexoProductOptions
{
    public const string SectionPath = "Nexo:Product";

    /// <summary>Used when <c>X-Nexo-Tenant</c> is omitted.</summary>
    public string DefaultTenantId { get; set; } = "default";

    /// <summary>
    /// When non-empty, tenant header values must match one of these entries (case-sensitive trim).
    /// When empty, any non-empty tenant header up to 128 chars is accepted after trimming.
    /// </summary>
    public string[] AllowedTenantIds { get; set; } = [];

    /// <summary>Cloud mode: copilot/usage routes require <c>X-Nexo-User</c> + <c>X-Nexo-Org</c> membership.</summary>
    public bool RequireOrgMembership { get; set; }

    public string UserHeaderName { get; set; } = NexoHttpOrg.UserHeaderName;

    public string OrgHeaderName { get; set; } = NexoHttpOrg.OrgHeaderName;
}
