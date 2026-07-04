namespace Nexo.Core.Application.Environments;

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
