using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Multiplayer;

/// <summary>Result of subscribing to scoped pose updates.</summary>
public sealed class SubscribeResult
{
    public SubscribeResult(bool succeeded, IObservable<ScopedPoseMessage>? stream, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        Stream = stream;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    /// <summary>Whether the subscription was established successfully.</summary>
    public bool Succeeded { get; }

    /// <summary>Pose update stream when <see cref="Succeeded"/> is true.</summary>
    public IObservable<ScopedPoseMessage>? Stream { get; }

    /// <summary>Machine-readable rejection code when <see cref="Succeeded"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Human-readable rejection detail when <see cref="Succeeded"/> is false.</summary>
    public string? Reason { get; }

    /// <summary>Creates a successful subscription result.</summary>
    public static SubscribeResult Success(IObservable<ScopedPoseMessage> stream) => new(true, stream, null, null);

    /// <summary>Creates a rejected subscription result.</summary>
    public static SubscribeResult Rejected(string rejectionCode, string reason) =>
        new(false, null, rejectionCode, reason);
}
