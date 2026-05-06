namespace Nexo.API.Forge;

/// <summary>
/// Optional durable Forge session storage configuration.
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
    /// Maximum response body size (bytes) for HTTP <c>fetch_*</c> stages in <see cref="MapPipelineRunner"/>.
    /// </summary>
    public int MaxFetchResponseBytes { get; set; } = 2_097_152;

    /// <summary>
    /// When set, session and macros persist via LiteDB at this path (absolute or relative to content root).
    /// </summary>
    public string? LiteDbPath { get; set; }
}
