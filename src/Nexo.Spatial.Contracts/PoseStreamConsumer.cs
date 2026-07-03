namespace Nexo.Spatial.Contracts;

/// <summary>
/// Downstream consumer that applies confidence, gap-timeout, and velocity-bound policies to raw pose streams.
/// Smoothing and discontinuity handling live here rather than in <see cref="ISpatialAnchorProvider"/>.
/// </summary>
public sealed class PoseStreamConsumer
{
    public PoseStreamConsumer(PoseStreamConsumerOptions? options = null)
    {
        Options = options ?? new PoseStreamConsumerOptions();
    }

    public PoseStreamConsumerOptions Options { get; }

    public ProcessedPoseSample Process(PoseSample? current, PoseSample? previous, DateTimeOffset now)
    {
        if (current is null)
        {
            if (previous is not null
                && now - previous.Timestamp > Options.GapTimeout)
            {
                return ProcessedPoseSample.Lost("pose-gap-timeout");
            }

            return ProcessedPoseSample.Lost("pose-unavailable");
        }

        if (current.TrackingState == TrackingState.Lost)
            return ProcessedPoseSample.Lost("provider-lost");

        if (current.Confidence < Options.MinConfidence)
        {
            return new ProcessedPoseSample(
                Accepted: false,
                EffectiveTrackingState: TrackingState.Occluded,
                Pose: current,
                RejectionReason: "confidence-below-threshold");
        }

        if (previous is not null
            && now - previous.Timestamp > Options.GapTimeout)
        {
            return ProcessedPoseSample.Lost("pose-gap-timeout");
        }

        if (previous is not null && ExceedsVelocityBound(previous, current))
        {
            return new ProcessedPoseSample(
                Accepted: false,
                EffectiveTrackingState: TrackingState.Occluded,
                Pose: current,
                RejectionReason: "velocity-discontinuity");
        }

        return new ProcessedPoseSample(
            Accepted: true,
            EffectiveTrackingState: current.TrackingState,
            Pose: current,
            RejectionReason: null);
    }

    private bool ExceedsVelocityBound(PoseSample previous, PoseSample current)
    {
        var dt = (current.Timestamp - previous.Timestamp).TotalSeconds;
        if (dt <= 0)
            return true;

        var dx = current.Position.X - previous.Position.X;
        var dy = current.Position.Y - previous.Position.Y;
        var dz = current.Position.Z - previous.Position.Z;
        var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var velocity = distance / dt;
        return velocity > Options.MaxVelocityMetersPerSecond;
    }
}

/// <summary>
/// Policy thresholds for <see cref="PoseStreamConsumer"/>.
/// </summary>
public sealed record PoseStreamConsumerOptions
{
    public double MinConfidence { get; init; } = 0.5;

    public TimeSpan GapTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    public double MaxVelocityMetersPerSecond { get; init; } = 10.0;
}

/// <summary>
/// Consumer output after policy evaluation.
/// </summary>
public sealed record ProcessedPoseSample(
    bool Accepted,
    TrackingState EffectiveTrackingState,
    PoseSample? Pose,
    string? RejectionReason)
{
    public static ProcessedPoseSample Lost(string reason) =>
        new(false, TrackingState.Lost, null, reason);
}
