namespace Ashlar.Core.Application.Environments.Ports;

using Ashlar.Core.Application.Environments;

/// <summary>
/// Validates map inputs per LOD tier: water bodies closed, roads connected, terrain/voxel alignment.
/// Pass <see cref="MapVerificationRequest.ParentTierReport"/> to enforce tiling-down consistency between tiers.
/// </summary>
public interface IMapVerificationService
{
    /// <summary>
    /// Verifies map data consistency for the requested tier and geographic bounds.
    /// </summary>
    /// <param name="request">Bounds, tier, optional vector sample, and parent-tier report.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Validation report with issues and pass/fail outcome.</returns>
    Task<MapVerificationReport> VerifyAsync(
        MapVerificationRequest request,
        CancellationToken cancellationToken = default);
}
