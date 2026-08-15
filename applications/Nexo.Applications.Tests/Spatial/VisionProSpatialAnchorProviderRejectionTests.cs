using FluentAssertions;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.VisionPro;
using Nexo.Spatial.Platform.VisionPro.Interop;
using System.Reactive.Linq;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S3 Vision Pro provider rejection tests (headless CI — no visionOS SDK).
/// </summary>
[Trait("Category", "Spatial")]
public sealed class VisionProSpatialAnchorProviderRejectionTests
{
    [Fact]
    public async Task R1_UnsupportedHost_GetCurrentPose_ReturnsNullNotFabricatedPose()
    {
        VisionProSpatialAvailability.IsSupported().Should().BeFalse("headless CI is never visionOS");

        var provider = VisionProSpatialAnchorProvider.WithPlatformSession(
            sessionActive: true,
            immersiveSpaceActive: true);

        (await provider.GetCurrentPose("atom-a")).Should().BeNull();
    }

    [Fact]
    public async Task R2_UnsupportedHost_ObservePose_EmitsLostNotTracking()
    {
        var provider = VisionProSpatialAnchorProvider.WithPlatformSession(
            sessionActive: true,
            immersiveSpaceActive: true);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task R3_BlankAtomId_ObservePose_EmitsLost()
    {
        var provider = new VisionProSpatialAnchorProvider();

        (await provider.ObservePose("  ").FirstAsync()).TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R4_UninitializedSession_StillFailsClosed()
    {
        var provider = VisionProSpatialAnchorProvider.WithPlatformSession(sessionActive: false);

        (await provider.GetCurrentPose("atom-a")).Should().BeNull();
        (await provider.ObservePose("atom-a").FirstAsync()).TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R5_PreImmersiveSpace_FailsClosedWithoutException()
    {
        var session = new SeamVisionProNativeSession(active: true, immersiveSpaceActive: false);
        session.RegisterPose("atom-a", new VisionProNativePoseFrame(
            1, 0, 0, 0, 0, 0, 1,
            VisionProNativeTrackingQuality.Normal,
            DateTimeOffset.UtcNow));

        var provider = new VisionProSpatialAnchorProvider(session);

        var act = async () =>
        {
            (await provider.GetCurrentPose("atom-a")).Should().BeNull();
            (await provider.ObservePose("atom-a").FirstAsync()).TrackingState.Should().Be(TrackingState.Lost);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task R6_LimitedTracking_MapsToOccludedNotCleanTracking()
    {
        var session = new SeamVisionProNativeSession(active: true, immersiveSpaceActive: true);
        session.RegisterPose("atom-a", new VisionProNativePoseFrame(
            1, 2, 3, 0, 0, 0, 1,
            VisionProNativeTrackingQuality.Limited,
            DateTimeOffset.UtcNow));

        var provider = new VisionProSpatialAnchorProvider(session);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().NotBeNull();
        pose!.TrackingState.Should().Be(TrackingState.Occluded);
        pose.TrackingState.Should().NotBe(TrackingState.Tracking);
    }

    private sealed class SeamVisionProNativeSession : IVisionProNativeSession
    {
        private readonly Dictionary<string, VisionProNativePoseFrame> _poses = new(StringComparer.Ordinal);
        private readonly bool _active;
        private readonly bool _immersiveSpaceActive;

        public SeamVisionProNativeSession(bool active, bool immersiveSpaceActive)
        {
            _active = active;
            _immersiveSpaceActive = immersiveSpaceActive;
        }

        public bool IsActive => _active && _immersiveSpaceActive;

        public bool IsImmersiveSpaceActive => _immersiveSpaceActive;

        public bool IsInterrupted => false;

        public void RegisterPose(string atomId, VisionProNativePoseFrame frame) => _poses[atomId] = frame;

        public VisionProNativePoseFrame? TryGetAnchorPose(string atomId) =>
            IsActive && _poses.TryGetValue(atomId, out var frame) ? frame : null;

        public IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId) =>
            Observable.Empty<VisionProNativePoseFrame>();
    }
}
