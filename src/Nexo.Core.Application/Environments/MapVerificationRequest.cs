namespace Nexo.Core.Application.Environments;

/// <summary>
/// Request for hierarchical (tier-aware) verification: optional parent-tier report enables
/// “tiling down” checks—finer tiers should refine coarser topology without contradicting water/road shells.
/// <para>Includes optional <c>DataBinding</c> for source identification, optional <c>VectorSamplePayload</c>
/// for offline checks, and optional <c>ParentTierReport</c> when verifying tier N+1 against tier N.</para>
/// </summary>
public sealed record MapVerificationRequest(
    MapDataGeographicBounds Bounds,
    int TierIndex,
    string? EnvironmentManifestId,
    MapDataSourceBinding? DataBinding,
    ReadOnlyMemory<byte>? VectorSamplePayload,
    string? VectorFormatHint,
    MapDataRequestContext Context,
    MapVerificationReport? ParentTierReport = null);
