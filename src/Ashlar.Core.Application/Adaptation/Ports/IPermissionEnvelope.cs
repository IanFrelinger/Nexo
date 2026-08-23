namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Permission envelope driven by autonomy level.
/// Supervised: all require approval; SemiAutonomous: apply fixes, no auto-promote; FullyAutonomous: apply and promote.
/// </summary>
public interface IPermissionEnvelope
{
    /// <summary>Whether source-level fixes can be applied without user approval.</summary>
    bool CanApplySourceFix { get; }

    /// <summary>Whether brick-level fixes can be applied without user approval.</summary>
    bool CanApplyBrickFix { get; }

    /// <summary>Whether fixes can be promoted (validated and kept) without user approval.</summary>
    bool CanPromoteWithoutApproval { get; }
}
