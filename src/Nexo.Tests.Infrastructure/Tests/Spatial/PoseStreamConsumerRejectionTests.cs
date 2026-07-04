using FluentAssertions;
using Nexo.Spatial.Contracts;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>Tests for pose stream consumer rejection.</summary>
[Trait("Category", "Spatial")]
public sealed class PoseStreamConsumerRejectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void R1_ConfidenceBelowThreshold_TreatedAsOccluded()
    {
        var consumer = new PoseStreamConsumer(new PoseStreamConsumerOptions { MinConfidence = 0.7 });
        var sample = CreateSample(T0, confidence: 0.2, TrackingState.Tracking);

        var processed = consumer.Process(sample, previous: null, now: T0);

        processed.Accepted.Should().BeFalse();
        processed.EffectiveTrackingState.Should().Be(TrackingState.Occluded);
        processed.RejectionReason.Should().Be("confidence-below-threshold");
    }

    [Fact]
    public void R2_GapLongerThanTimeout_ReportsLostNotStalePose()
    {
        var consumer = new PoseStreamConsumer(new PoseStreamConsumerOptions { GapTimeout = TimeSpan.FromMilliseconds(100) });
        var previous = CreateSample(T0, confidence: 0.9, TrackingState.Tracking);
        var now = T0.AddMilliseconds(250);

        var processed = consumer.Process(current: null, previous, now);

        processed.EffectiveTrackingState.Should().Be(TrackingState.Lost);
        processed.Pose.Should().BeNull();
        processed.RejectionReason.Should().Be("pose-gap-timeout");
    }

    [Fact]
    public void R3_RapidPoseJump_FlagsDiscontinuity()
    {
        var consumer = new PoseStreamConsumer(new PoseStreamConsumerOptions { MaxVelocityMetersPerSecond = 1.0 });
        var previous = CreateSample(T0, confidence: 0.9, TrackingState.Tracking, x: 0);
        var current = CreateSample(T0.AddMilliseconds(100), confidence: 0.9, TrackingState.Tracking, x: 5);

        var processed = consumer.Process(current, previous, current.Timestamp);

        processed.Accepted.Should().BeFalse();
        processed.RejectionReason.Should().Be("velocity-discontinuity");
    }

    private static PoseSample CreateSample(
        DateTimeOffset timestamp,
        double confidence,
        TrackingState trackingState,
        double x = 0,
        double y = 0,
        double z = 0) =>
        new(
            new SpatialVector3(x, y, z),
            new SpatialQuaternion(0, 0, 0, 1),
            confidence,
            timestamp,
            trackingState);
}
