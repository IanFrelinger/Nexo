namespace Ashlar.Spatial.Contracts;

/// <summary>
/// Policy thresholds for <see cref="PoseStreamConsumer"/>.
/// </summary>
public sealed record PoseStreamConsumerOptions
{
    /// <summary>Minimum provider confidence required to accept a sample.</summary>
    public double MinConfidence { get; init; } = 0.5;

    /// <summary>Maximum elapsed time without a sample before tracking is considered lost.</summary>
    public TimeSpan GapTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum implied velocity (m/s) between consecutive samples.</summary>
    public double MaxVelocityMetersPerSecond { get; init; } = 10.0;
}
