namespace Ashlar.Core.Application.Environments;

/// <summary>
/// Request for hierarchical (tier-aware) verification: optional parent-tier report enables
/// “tiling down” checks—finer tiers should refine coarser topology without contradicting water/road shells.
/// <para>Includes optional <c>DataBinding</c> for source identification, optional <c>VectorSamplePayload</c>
/// for offline checks, and optional <c>ParentTierReport</c> when verifying tier N+1 against tier N.</para>
/// </summary>
/// <param name="Bounds">Geographic area under verification.</param>
/// <param name="TierIndex">LOD tier under test.</param>
/// <param name="EnvironmentManifestId">Optional environment manifest identifier.</param>
/// <param name="DataBinding">Optional data source binding for provenance.</param>
/// <param name="VectorSamplePayload">Optional offline vector sample bytes.</param>
/// <param name="VectorFormatHint">Format of the vector sample (e.g. osm-xml, geojson).</param>
/// <param name="Context">Cross-cutting request context for tracing and hints.</param>
/// <param name="ParentTierReport">Optional coarser-tier report for tiling-down consistency checks.</param>
public sealed record MapVerificationRequest(
    MapDataGeographicBounds Bounds,
    int TierIndex,
    string? EnvironmentManifestId,
    MapDataSourceBinding? DataBinding,
    ReadOnlyMemory<byte>? VectorSamplePayload,
    string? VectorFormatHint,
    MapDataRequestContext Context,
    MapVerificationReport? ParentTierReport = null);
