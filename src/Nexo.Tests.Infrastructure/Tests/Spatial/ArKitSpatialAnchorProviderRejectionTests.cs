using FluentAssertions;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.ARKit;
using Nexo.Spatial.Platform.ARKit.Interop;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S1 ARKit provider rejection tests (headless CI — no device SDK).
/// </summary>
[Trait("Category", "Spatial")]
public sealed class ArKitSpatialAnchorProviderRejectionTests
{
    [Fact]
    public async Task R1_NonIosHost_GetCurrentPose_ReturnsNullNotFabricatedPose()
    {
        ArKitSpatialAvailability.IsSupported().Should().BeFalse("headless CI is never iOS");

        var provider = ArKitSpatialAnchorProvider.WithPlatformSession(sessionActive: true);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().BeNull();
    }

    [Fact]
    public async Task R2_NonIosHost_ObservePose_EmitsLostNotTracking()
    {
        var provider = ArKitSpatialAnchorProvider.WithPlatformSession(sessionActive: true);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task R3_BlankAtomId_ObservePose_EmitsLost()
    {
        var provider = new ArKitSpatialAnchorProvider();

        var sample = await provider.ObservePose("  ").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R4_UninitializedSession_OnNonIos_StillFailsClosed()
    {
        var provider = ArKitSpatialAnchorProvider.WithPlatformSession(sessionActive: false);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().BeNull();
        var sample = await provider.ObservePose("atom-a").FirstAsync();
        sample.TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R5_LimitedTracking_MapsToOccludedNotCleanTracking()
    {
        var session = new SeamArKitNativeSession(active: true);
        session.RegisterPose("atom-a", new ArKitNativePoseFrame(
            1, 2, 3, 0, 0, 0, 1,
            ArKitNativeTrackingQuality.Limited,
            DateTimeOffset.UtcNow));

        var provider = new ArKitSpatialAnchorProvider(session);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().NotBeNull();
        pose!.TrackingState.Should().Be(TrackingState.Occluded);
        pose.TrackingState.Should().NotBe(TrackingState.Tracking);
        pose.Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task R6_SessionInterrupted_ObservePose_EmitsLostNotSilentStop()
    {
        var session = new SeamArKitNativeSession(active: true);
        session.RegisterPose("atom-a", new ArKitNativePoseFrame(
            1, 0, 0, 0, 0, 0, 1,
            ArKitNativeTrackingQuality.Normal,
            DateTimeOffset.UtcNow));

        var provider = new ArKitSpatialAnchorProvider(session);
        session.SetInterrupted(true);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task R7_UnregisteredAtomId_GetCurrentPose_ReturnsNullNotZeroedPose()
    {
        var session = new SeamArKitNativeSession(active: true);
        var provider = new ArKitSpatialAnchorProvider(session);

        var pose = await provider.GetCurrentPose("missing-atom");

        pose.Should().BeNull();
    }

    private sealed class SeamArKitNativeSession : IArKitNativeSession
    {
        private readonly Dictionary<string, ArKitNativePoseFrame> _poses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Subject<ArKitNativePoseFrame>> _streams = new(StringComparer.Ordinal);
        private bool _active;
        private bool _interrupted;

        public SeamArKitNativeSession(bool active) => _active = active;

        public bool IsActive => _active && !_interrupted;

        public bool IsInterrupted => _interrupted;

        public void RegisterPose(string atomId, ArKitNativePoseFrame frame) =>
            _poses[atomId] = frame;

        public void SetInterrupted(bool interrupted) => _interrupted = interrupted;

        public ArKitNativePoseFrame? TryGetAnchorPose(string atomId) =>
            _poses.TryGetValue(atomId, out var frame) ? frame : null;

        public IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId)
        {
            if (_interrupted)
            {
                return Observable.Return(new ArKitNativePoseFrame(
                    0, 0, 0, 0, 0, 0, 1,
                    ArKitNativeTrackingQuality.NotAvailable,
                    DateTimeOffset.UtcNow));
            }

            if (!_streams.TryGetValue(atomId, out var subject))
            {
                subject = new Subject<ArKitNativePoseFrame>();
                _streams[atomId] = subject;
            }

            return subject.AsObservable();
        }
    }
}
