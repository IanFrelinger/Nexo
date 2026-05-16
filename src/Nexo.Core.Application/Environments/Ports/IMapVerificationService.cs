namespace Nexo.Core.Application.Environments.Ports;

using Nexo.Core.Application.Environments;

/// <summary>
/// Validates map inputs per LOD tier: water bodies closed, roads connected, terrain/voxel alignment.
/// Pass <see cref="MapVerificationRequest.ParentTierReport"/> to enforce tiling-down consistency between tiers.
/// </summary>
public interface IMapVerificationService
{
    Task<MapVerificationReport> VerifyAsync(
        MapVerificationRequest request,
        CancellationToken cancellationToken = default);
}
