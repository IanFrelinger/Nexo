namespace Nexo.API.Security;

/// <summary>
/// Private deployment license gate (Product Fleet Phase 1.2).
/// </summary>
public sealed class NexoPrivateLicenseOptions
{
    public const string SectionPath = "Nexo:PrivateLicense";

    /// <summary>When false, license checks are advisory only (logged, not enforced).</summary>
    public bool EnforceLicense { get; set; }

    /// <summary>Path to signed license JSON. Overridden by <c>NEXO_LICENSE_FILE</c> when set.</summary>
    public string? LicenseFilePath { get; set; }

    /// <summary>Optional HMAC secret for verifying the <c>signature</c> field in the license file.</summary>
    public string? HmacSecret { get; set; }

    /// <summary>When license is expired, allow GET/read APIs but block mutating routes.</summary>
    public bool AllowReadOnlyWhenExpired { get; set; } = true;
}
