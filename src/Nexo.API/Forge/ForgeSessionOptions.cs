namespace Nexo.API.Forge;

/// <summary>
/// Forge session storage, tenant header, map pipeline fetch policy, and vector intelligence options.
/// </summary>
public sealed class ForgeSessionOptions
{
    public const string SectionPath = "Nexo:ForgeSession";

    /// <summary>
    /// HTTP header name for optional Forge tenant isolation (default <c>X-Forge-Tenant</c>).
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Forge-Tenant";

    /// <summary>
    /// When true, non-dry map pipeline runs may call <see cref="Nexo.GameDomain.Mapping.IVectorMapIntelligenceService"/> on fetched vector bytes.
    /// </summary>
    public bool EnableVectorIntelligence { get; set; }

    /// <summary>
    /// When true and <see cref="EnableVectorIntelligence"/> is true, runs a short <see cref="Nexo.Abstractions.IModel"/> prompt after heuristics.
    /// </summary>
    public bool EnableVectorModel { get; set; }

    /// <summary>Max characters of heuristic notes sent to the model.</summary>
    public int MaxVectorModelPromptChars { get; set; } = 4000;

    /// <summary>Timeout for model augmentation (milliseconds).</summary>
    public int VectorModelTimeoutMs { get; set; } = 15_000;

    /// <summary>
    /// Maximum response body size (bytes) for HTTP <c>fetch_*</c> stages in <see cref="MapPipelineRunner"/>.
    /// </summary>
    public int MaxFetchResponseBytes { get; set; } = 2_097_152;

    /// <summary>
    /// Host allowlist for map pipeline HTTP fetches (e.g. <c>api.mapbox.com</c>, <c>*.tile.openstreetmap.org</c>).
    /// Empty list blocks fetches unless <see cref="AllowMapFetchWhenAllowedHostsEmpty"/> is true.
    /// </summary>
    public List<string> AllowedMapFetchHosts { get; set; } = [];

    /// <summary>
    /// When true, allows fetches when <see cref="AllowedMapFetchHosts"/> is empty (insecure; dev/tests only).
    /// </summary>
    public bool AllowMapFetchWhenAllowedHostsEmpty { get; set; }

    /// <summary>When true, allows http:// URLs for map fetches (default https only).</summary>
    public bool AllowInsecureMapFetch { get; set; }

    /// <summary>When true, follows HTTP redirects for map fetches (default false).</summary>
    public bool AllowHttpRedirectsForMapFetch { get; set; }

    /// <summary>
    /// When true and the caller is authenticated, the Forge tenant id is taken from the claim
    /// <see cref="TenantClaimType"/> instead of the tenant header.
    /// </summary>
    public bool BindTenantFromClaims { get; set; }

    /// <summary>Claim type for <see cref="BindTenantFromClaims"/> (e.g. <c>tid</c>).</summary>
    public string? TenantClaimType { get; set; }

    /// <summary>
    /// When <see cref="BindTenantFromClaims"/> is true and this is false (default), unauthenticated callers
    /// cannot fall back to <see cref="TenantHeaderName"/>; they receive 401 instead.
    /// </summary>
    public bool AllowTenantHeaderWhenClaimsBindingEnabled { get; set; }

    /// <summary>
    /// When true, requests under <c>/api/forge</c> must be authenticated (via Nexo built-in auth).
    /// </summary>
    public bool RequireForgeAuthentication { get; set; }

    /// <summary>
    /// When true (default), <see cref="MapPipelineRunner"/> records lightweight GeoJSON / OSM / MVT parse stats on <c>fetch_vector</c>.
    /// </summary>
    public bool EnableVectorPayloadParsing { get; set; } = true;

    /// <summary>
    /// When set, session and macros persist via LiteDB at this path (absolute or relative to content root).
    /// </summary>
    public string? LiteDbPath { get; set; }
}
