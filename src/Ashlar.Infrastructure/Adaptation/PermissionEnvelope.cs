using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation;

/// <summary>
/// Permission envelope driven by autonomy level string.
/// Supervised: all require approval; Semi: apply fixes, no auto-promote; Full: apply and promote.
/// </summary>
public sealed class PermissionEnvelope : IPermissionEnvelope
{
    private readonly string _level;

    /// <summary>Initializes a new permission envelope.</summary>
    public PermissionEnvelope(string autonomyLevel)
    {
        _level = (autonomyLevel ?? "supervised").Trim().ToLowerInvariant();
    }

    /// <inheritdoc />
    public bool CanApplySourceFix => _level is "semi" or "semi-autonomous" or "semiautonomous" or "full" or "fully-autonomous" or "fullyautonomous";

    /// <inheritdoc />
    public bool CanApplyBrickFix => _level is "semi" or "semi-autonomous" or "semiautonomous" or "full" or "fully-autonomous" or "fullyautonomous";

    /// <inheritdoc />
    public bool CanPromoteWithoutApproval => _level is "full" or "fully-autonomous" or "fullyautonomous";
}
