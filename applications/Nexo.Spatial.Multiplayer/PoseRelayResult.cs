using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Multiplayer;

/// <summary>
/// Result of attempting to publish a scoped pose.
/// </summary>
public sealed class PoseRelayResult
{
    public PoseRelayResult(bool accepted, string? rejectionCode, string? reason)
    {
        Accepted = accepted;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    /// <summary>Whether the pose publish was accepted by the relay.</summary>
    public bool Accepted { get; }

    /// <summary>Machine-readable rejection code when <see cref="Accepted"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Human-readable rejection detail when <see cref="Accepted"/> is false.</summary>
    public string? Reason { get; }

    /// <summary>Creates an accepted publish result.</summary>
    public static PoseRelayResult AcceptedResult() => new(true, null, null);

    /// <summary>Creates a rejected publish result.</summary>
    public static PoseRelayResult Rejected(string rejectionCode, string reason) =>
        new(false, rejectionCode, reason);
}
