namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Consumer output after policy evaluation.
/// </summary>
/// <param name="Accepted">Whether the sample passed all policy checks.</param>
/// <param name="EffectiveTrackingState">Tracking state after policy evaluation.</param>
/// <param name="Pose">Original or last observed pose, when available.</param>
/// <param name="RejectionReason">Canonical rejection reason when <paramref name="Accepted"/> is false.</param>
public sealed record ProcessedPoseSample(
    bool Accepted,
    TrackingState EffectiveTrackingState,
    PoseSample? Pose,
    string? RejectionReason)
{
    /// <summary>Creates a lost-tracking result with a canonical reason code.</summary>
    /// <param name="reason">Rejection or loss reason code.</param>
    public static ProcessedPoseSample Lost(string reason) =>
        new(false, TrackingState.Lost, null, reason);
}
