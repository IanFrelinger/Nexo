using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Runtime.Ports;

namespace Nexo.Spatial.Runtime;

/// <summary>
/// Continuous bound pose update emitted by <see cref="SpatialBindingService"/>.
/// </summary>
public sealed class SpatialBindingUpdate
{
    public SpatialBindingUpdate(
        bool active,
        bool lost,
        string? rejectionCode,
        ProcessedPoseSample? processedPose,
        ResolvedAtomIdentity? identity)
    {
        Active = active;
        Lost = lost;
        RejectionCode = rejectionCode?.Trim();
        ProcessedPose = processedPose;
        Identity = identity;
    }

    /// <summary>Whether the binding update represents an active tracked pose.</summary>
    public bool Active { get; }

    /// <summary>Whether tracking was lost since the previous update.</summary>
    public bool Lost { get; }

    /// <summary>Machine-readable rejection or loss reason when not active.</summary>
    public string? RejectionCode { get; }

    /// <summary>Processed pose sample when the update is active.</summary>
    public ProcessedPoseSample? ProcessedPose { get; }

    /// <summary>Certified atom identity for the bound pose.</summary>
    public ResolvedAtomIdentity? Identity { get; }

    /// <summary>Creates a rejected binding update.</summary>
    public static SpatialBindingUpdate RejectedUpdate(string rejectionCode) =>
        new(false, false, rejectionCode, null, null);

    /// <summary>Creates a lost-tracking binding update.</summary>
    public static SpatialBindingUpdate LostUpdate(string reason) =>
        new(false, true, reason, null, null);

    /// <summary>Creates an active binding update with pose and identity.</summary>
    public static SpatialBindingUpdate ActiveUpdate(ProcessedPoseSample processedPose, ResolvedAtomIdentity identity) =>
        new(true, false, null, processedPose, identity);
}
