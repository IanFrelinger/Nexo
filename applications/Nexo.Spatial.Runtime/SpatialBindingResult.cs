using Nexo.Spatial.Contracts;
using Nexo.Spatial.Runtime.Ports;

namespace Nexo.Spatial.Runtime;

/// <summary>
/// Result of attempting to bind pose tracking to a certified physical atom.
/// </summary>
public sealed class SpatialBindingResult
{
    public SpatialBindingResult(
        bool active,
        string? rejectionCode,
        ProcessedPoseSample? processedPose,
        ResolvedAtomIdentity? identity)
    {
        Active = active;
        RejectionCode = rejectionCode?.Trim();
        ProcessedPose = processedPose;
        Identity = identity;
    }

    /// <summary>Whether pose binding is active for the certified atom.</summary>
    public bool Active { get; }

    /// <summary>Machine-readable rejection code when <see cref="Active"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Latest processed pose sample when binding is active.</summary>
    public ProcessedPoseSample? ProcessedPose { get; }

    /// <summary>Certified atom identity associated with the binding.</summary>
    public ResolvedAtomIdentity? Identity { get; }

    /// <summary>Creates a rejected binding result.</summary>
    public static SpatialBindingResult Rejected(string rejectionCode) =>
        new(false, rejectionCode, null, null);

    /// <summary>Creates an active binding result with pose and identity.</summary>
    public static SpatialBindingResult ActiveBinding(ProcessedPoseSample processedPose, ResolvedAtomIdentity identity) =>
        new(true, null, processedPose, identity);
}
