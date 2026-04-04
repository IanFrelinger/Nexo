namespace Nexo.API.Security;

/// <summary>
/// User-configurable security posture hints for Nexo.API (appsettings / environment). Does not replace firewalls or Tailscale ACLs.
/// </summary>
public sealed class NexoSecurityOptions
{
    public const string SectionPath = "Nexo:Security";

    /// <summary>
    /// One of: Localhost, Lan, Tailnet, Public (case-insensitive).
    /// </summary>
    public string ExposureProfile { get; set; } = "Localhost";

    /// <summary>
    /// Optional extra line appended to the portal advisory (e.g. team policy or on-call).
    /// </summary>
    public string? CustomAdvisory { get; set; }

    /// <summary>
    /// When true, mutating API routes (POST/PUT/PATCH/DELETE under /api) require an API key header.
    /// </summary>
    public bool RequireApiKeyForMutatingEndpoints { get; set; }

    /// <summary>
    /// Shared secret value expected in <see cref="ApiKeyHeaderName"/> for protected requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Header name used for API key checks.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-Nexo-Api-Key";

    /// <summary>
    /// Optional API route prefixes (e.g., /api/director/run) that bypass API key checks when
    /// <see cref="RequireApiKeyForMutatingEndpoints"/> is true.
    /// </summary>
    public string[] ExcludedApiKeyPaths { get; set; } = [];

    /// <summary>
    /// When false, <c>/api/security/advisory</c> still works but the portal hides the banner.
    /// </summary>
    public bool ShowAdvisoryInPortal { get; set; } = true;
}
