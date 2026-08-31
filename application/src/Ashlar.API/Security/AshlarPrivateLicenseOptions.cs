namespace Ashlar.API.Security;

/// <summary>
/// Private deployment license gate (Product Fleet Phase 1.2).
/// </summary>
public sealed class AshlarPrivateLicenseOptions
{
    /// <summary>Configuration section path (<c>Ashlar:PrivateLicense</c>).</summary>
    public const string SectionPath = "Ashlar:PrivateLicense";

    /// <summary>When false, license checks are advisory only (logged, not enforced).</summary>
    public bool EnforceLicense { get; set; }

    /// <summary>Path to signed license JSON. Overridden by <c>ASHLAR_LICENSE_FILE</c> when set.</summary>
    public string? LicenseFilePath { get; set; }

    /// <summary>Optional HMAC secret for verifying the <c>signature</c> field in the license file.</summary>
    public string? HmacSecret { get; set; }

    /// <summary>When the license is <b>Invalid</b>, allow GET/read APIs but block mutating
    /// routes. An <b>Expired</b> license always degrades to read-only regardless of this
    /// flag — the lapsed floor is a <c>LICENSING.md</c> commitment, not a configuration.</summary>
    public bool AllowReadOnlyWhenExpired { get; set; } = true;
}
