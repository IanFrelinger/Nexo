namespace Nexo.API.Security;

/// <summary>
/// User-configurable security posture hints for Nexo.API (appsettings / environment). Does not replace firewalls or Tailscale ACLs.
/// </summary>
public sealed class NexoSecurityOptions
{
    /// <summary>Configuration section path (<c>Nexo:Security</c>).</summary>
    public const string SectionPath = "Nexo:Security";

    /// <summary>
    /// One of: Localhost, Lan, Tailnet, Public (case-insensitive).
    /// Lan, Tailnet and Public refuse to start when no built-in auth is configured unless
    /// <see cref="AllowUnauthenticatedNetworkExposure"/> is set explicitly.
    /// </summary>
    public string ExposureProfile { get; set; } = "Localhost";

    /// <summary>
    /// Escape hatch for the exposure fail-closed rule: when true, a Lan/Tailnet/Public
    /// <see cref="ExposureProfile"/> with <see cref="AuthorizationMode"/> None only logs instead of
    /// throwing at startup. Set it only when an authenticating proxy or network ACL fronts the API.
    /// </summary>
    public bool AllowUnauthenticatedNetworkExposure { get; set; }

    /// <summary>
    /// Optional extra line appended to the portal advisory (e.g. team policy or on-call).
    /// </summary>
    public string? CustomAdvisory { get; set; }

    /// <summary>
    /// When true, mutating API routes (POST/PUT/PATCH/DELETE under /api) require an API key header.
    /// Fails closed: with no <see cref="ApiKey"/> configured every mutating request is rejected (401).
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
    /// Optional API route prefixes that bypass built-in authorization checks
    /// (for example, <c>/api/security/advisory</c>).
    /// </summary>
    public string[] ExcludedAuthorizationPaths { get; set; } = [];

    /// <summary>
    /// When false, <c>/api/security/advisory</c> still works but the portal hides the banner.
    /// </summary>
    public bool ShowAdvisoryInPortal { get; set; } = true;

    /// <summary>
    /// Optional built-in authorization mode for API routes.
    /// Values: None, ApiKey, BearerToken, Basic, ApiKeyOrBearerToken, ApiKeyOrBasic, BearerTokenOrBasic, Any.
    /// When set to None, legacy <see cref="RequireApiKeyForMutatingEndpoints"/> behavior still applies.
    /// </summary>
    public string AuthorizationMode { get; set; } = nameof(NexoAuthorizationMode.None);

    /// <summary>
    /// Scope for built-in authorization checks: MutatingApi or AllApi.
    /// </summary>
    public string AuthorizationScope { get; set; } = nameof(NexoAuthorizationScope.MutatingApi);

    /// <summary>
    /// Shared secret value for built-in Bearer token authorization.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Header name used for Bearer token checks.
    /// Defaults to Authorization (expects "Bearer &lt;token&gt;" format).
    /// </summary>
    public string BearerTokenHeaderName { get; set; } = "Authorization";

    /// <summary>
    /// Scheme prefix expected when <see cref="BearerTokenHeaderName"/> is Authorization.
    /// </summary>
    public string BearerTokenScheme { get; set; } = "Bearer";

    /// <summary>
    /// Username for built-in Basic authorization.
    /// </summary>
    public string? BasicAuthUsername { get; set; }

    /// <summary>
    /// Password for built-in Basic authorization.
    /// </summary>
    public string? BasicAuthPassword { get; set; }

    /// <summary>
    /// Header name used for Basic authorization checks.
    /// Defaults to Authorization (expects "Basic &lt;base64(username:password)&gt;").
    /// </summary>
    public string BasicAuthHeaderName { get; set; } = "Authorization";

    /// <summary>
    /// Optional API key that may only call copilot history/onboarding/status endpoints (see middleware).
    /// When omitted, only <see cref="ApiKey"/> grants access.
    /// </summary>
    public string? CopilotScopedApiKey { get; set; }

    /// <summary>
    /// When built-in auth is active and true, GET <c>/api/copilot/tasks*</c> requires the same credentials as mutating routes.
    /// </summary>
    public bool RequireAuthForCopilotReadApis { get; set; } = true;
}
