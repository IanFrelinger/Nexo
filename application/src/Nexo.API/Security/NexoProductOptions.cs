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
}
