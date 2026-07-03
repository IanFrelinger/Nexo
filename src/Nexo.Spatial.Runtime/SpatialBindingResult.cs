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

    public bool Active { get; }

    public string? RejectionCode { get; }

    public ProcessedPoseSample? ProcessedPose { get; }

    public ResolvedAtomIdentity? Identity { get; }

    public static SpatialBindingResult Rejected(string rejectionCode) =>
        new(false, rejectionCode, null, null);

    public static SpatialBindingResult ActiveBinding(ProcessedPoseSample processedPose, ResolvedAtomIdentity identity) =>
        new(true, null, processedPose, identity);
}
