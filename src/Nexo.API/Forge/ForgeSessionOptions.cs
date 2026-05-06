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

    /// <summary>
    /// When set, session and macros persist via LiteDB at this path (absolute or relative to content root).
    /// </summary>
    public string? LiteDbPath { get; set; }
}
