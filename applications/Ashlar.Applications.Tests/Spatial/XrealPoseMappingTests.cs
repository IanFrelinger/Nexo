using FluentAssertions;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Platform.XREAL;
using Ashlar.Spatial.Platform.XREAL.Interop;
using System.Reactive.Linq;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S2 XREAL pose-mapping acceptance tests. The NRSDK frame → <see cref="PoseSample"/>
/// mapper is internal, so it is exercised through its only public entry point,
/// <see cref="XrealSpatialAnchorProvider"/>, with a scripted session seam.
/// </summary>
[Trait("Category", "Spatial")]
public sealed class XrealPoseMappingTests
{
    private static readonly DateTimeOffset FrameTime =
        new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A1_TrackingFrame_MapsPositionRotationTimestampAndHighConfidence()
    {
        var session = new ScriptedXrealNativeSession { Active = true };
        session.RegisterPose("atom-a", Frame(XrealNativeTrackingQuality.Tracking));
        var provider = new XrealSpatialAnchorProvider(session);

        var sample = await provider.GetCurrentPose("atom-a");

        sample.Should().NotBeNull();
        sample!.Position.Should().Be(new SpatialVector3(1.5, -2.25, 3.75));
        sample.Rotation.Should().Be(new SpatialQuaternion(0.1, 0.2, 0.3, 0.9));
        sample.Confidence.Should().Be(0.9);
        sample.Timestamp.Should().Be(FrameTime);
        sample.TrackingState.Should().Be(TrackingState.Tracking);
    }

    [Fact]
    public async Task A2_LimitedFrame_MapsToOccludedWithReducedConfidence()
    {
        var session = new ScriptedXrealNativeSession { Active = true };
        session.RegisterPose("atom-a", Frame(XrealNativeTrackingQuality.Limited));
        var provider = new XrealSpatialAnchorProvider(session);

        var sample = await provider.GetCurrentPose("atom-a");

        sample.Should().NotBeNull();
        sample!.TrackingState.Should().Be(TrackingState.Occluded);
        sample.Confidence.Should().Be(0.3);
    }

    [Fact]
    public async Task A3_LostFrame_MapsToLostWithZeroConfidence()
    {
        var session = new ScriptedXrealNativeSession { Active = true };
        session.RegisterPose("atom-a", Frame(XrealNativeTrackingQuality.Lost));
        var provider = new XrealSpatialAnchorProvider(session);

        var sample = await provider.GetCurrentPose("atom-a");

        sample.Should().NotBeNull();
        sample!.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task A4_ObservePose_EmitsInitialPoseThenStreamFrames()
    {
        var session = new ScriptedXrealNativeSession { Active = true };
        session.RegisterPose("atom-a", Frame(XrealNativeTrackingQuality.Tracking));
        session.SetStream("atom-a", Observable.Return(Frame(XrealNativeTrackingQuality.Limited)));
        var provider = new XrealSpatialAnchorProvider(session);

        var samples = await provider.ObservePose("atom-a").ToList();

        samples.Should().HaveCount(2);
        samples[0].TrackingState.Should().Be(TrackingState.Tracking);
        samples[0].Confidence.Should().Be(0.9);
        samples[1].TrackingState.Should().Be(TrackingState.Occluded);
        samples[1].Confidence.Should().Be(0.3);
    }

    [Fact]
    public async Task A5_ObservePose_NoInitialPoseAndEmptyStream_FallsBackToSingleLostSample()
    {
        var session = new ScriptedXrealNativeSession { Active = true };
        var provider = new XrealSpatialAnchorProvider(session);

        var samples = await provider.ObservePose("atom-a").ToList();

        samples.Should().HaveCount(1);
        samples[0].TrackingState.Should().Be(TrackingState.Lost);
        samples[0].Confidence.Should().Be(0);
    }

    [Fact]
    public async Task R1_ActiveSessionButHostDisconnected_ObservePose_EmitsLostNotStalePose()
    {
        var session = new ScriptedXrealNativeSession { Active = true, HostDisconnected = true };
        session.RegisterPose("atom-a", Frame(XrealNativeTrackingQuality.Tracking));
        var provider = new XrealSpatialAnchorProvider(session);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public void F1_PoseFrame_ExposesRawComponentsUnfiltered()
    {
        var frame = Frame(XrealNativeTrackingQuality.Limited);

        frame.PositionX.Should().Be(1.5);
        frame.PositionY.Should().Be(-2.25);
        frame.PositionZ.Should().Be(3.75);
        frame.RotationX.Should().Be(0.1);
        frame.RotationY.Should().Be(0.2);
        frame.RotationZ.Should().Be(0.3);
        frame.RotationW.Should().Be(0.9);
        frame.TrackingQuality.Should().Be(XrealNativeTrackingQuality.Limited);
        frame.Timestamp.Should().Be(FrameTime);
    }

    [Fact]
    public void F2_PoseFrame_ValueEqualityFollowsComponents()
    {
        var a = Frame(XrealNativeTrackingQuality.Tracking);
        var b = Frame(XrealNativeTrackingQuality.Tracking);

        a.Should().Be(b);
        (a with { TrackingQuality = XrealNativeTrackingQuality.Lost }).Should().NotBe(b);
        (a with { PositionX = 99 }).Should().NotBe(b);
    }

    private static XrealNativePoseFrame Frame(XrealNativeTrackingQuality quality) =>
        new(1.5, -2.25, 3.75, 0.1, 0.2, 0.3, 0.9, quality, FrameTime);

    /// <summary>
    /// Session seam with independently scriptable Active/HostDisconnected flags —
    /// unlike the platform session, disconnect does not force IsActive false, so the
    /// provider's own disconnect guard is observable.
    /// </summary>
    private sealed class ScriptedXrealNativeSession : IXrealNativeSession
    {
        private readonly Dictionary<string, XrealNativePoseFrame> _poses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IObservable<XrealNativePoseFrame>> _streams = new(StringComparer.Ordinal);

        public bool Active { get; init; }

        public bool HostDisconnected { get; init; }

        public bool IsActive => Active;

        public bool IsHostDisconnected => HostDisconnected;

        public void RegisterPose(string atomId, XrealNativePoseFrame frame) => _poses[atomId] = frame;

        public void SetStream(string atomId, IObservable<XrealNativePoseFrame> stream) => _streams[atomId] = stream;

        public XrealNativePoseFrame? TryGetAnchorPose(string atomId) =>
            _poses.TryGetValue(atomId, out var frame) ? frame : null;

        public IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId) =>
            _streams.TryGetValue(atomId, out var stream) ? stream : Observable.Empty<XrealNativePoseFrame>();
    }
}
