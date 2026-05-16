namespace Nexo.Core.Application.Environments;

/// <summary>
/// One issue found during map data verification (topology, hydrology, routing).
/// </summary>
public sealed record MapVerificationIssue(
    string Category,
    string Severity,
    string Message,
    IReadOnlyDictionary<string, string>? Evidence = null);

/// <summary>
/// Report from validating vector/terrain/voxel consistency at a pyramid tier.
/// </summary>
/// <param name="TierIndex">LOD tier under test (see <c>VoxelLodTier.TierIndex</c>).</param>
/// <param name="Issues">Findings; empty when clean.</param>
/// <param name="PassedCoreChecks">False when blocking defects exist (use <see cref="MapVerificationSeverity.Error"/>).</param>
public sealed record MapVerificationReport(
    int TierIndex,
    IReadOnlyList<MapVerificationIssue> Issues,
    bool PassedCoreChecks);

/// <summary>Severity strings for <see cref="MapVerificationIssue.Severity"/>.</summary>
public static class MapVerificationSeverity
{
    public const string Info = "info";
    public const string Warning = "warn";
    public const string Error = "error";
}

/// <summary>Conventional <see cref="MapVerificationIssue.Category"/> values.</summary>
public static class MapVerificationCategories
{
    public const string Water = "water";
    public const string Road = "road";
    public const string Building = "building";
    public const string Terrain = "terrain";
    public const string Topology = "topology";
    public const string VoxelChunk = "voxel_chunk";
}
