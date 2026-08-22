namespace Ashlar.API.Security;

/// <summary>
/// Tenant identity defaults for API-scoped persistence (copilot history, onboarding checks).
/// </summary>
public sealed class AshlarProductOptions
{
    /// <summary>Configuration section path (<c>Ashlar:Product</c>).</summary>
    public const string SectionPath = "Ashlar:Product";

    /// <summary>Used when <c>X-Ashlar-Tenant</c> is omitted.</summary>
    public string DefaultTenantId { get; set; } = "default";

    /// <summary>
    /// When non-empty, tenant header values must match one of these entries (case-sensitive trim).
    /// When empty, any non-empty tenant header up to 128 chars is accepted after trimming.
    /// </summary>
    public string[] AllowedTenantIds { get; set; } = [];

    /// <summary>Cloud mode: copilot/usage routes require <c>X-Ashlar-User</c> + <c>X-Ashlar-Org</c> membership.</summary>
    public bool RequireOrgMembership { get; set; }

    /// <summary>Header name for resolving the authenticated user ID.</summary>
    public string UserHeaderName { get; set; } = AshlarHttpOrg.UserHeaderName;

    /// <summary>Header name for resolving organization membership.</summary>
    public string OrgHeaderName { get; set; } = AshlarHttpOrg.OrgHeaderName;
}
