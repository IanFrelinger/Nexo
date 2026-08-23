using FluentAssertions;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Platform.XREAL;
using Ashlar.Spatial.Platform.XREAL.Interop;
using System.Reactive.Linq;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S2 XREAL provider rejection tests (headless CI — no NRSDK).
/// </summary>
[Trait("Category", "Spatial")]
public sealed class XrealSpatialAnchorProviderRejectionTests
{
    [Fact]
    public async Task R1_UnsupportedHost_GetCurrentPose_ReturnsNullNotFabricatedPose()
    {
        XrealSpatialAvailability.IsSupported().Should().BeFalse("headless CI is never Android NRSDK host");

        var provider = XrealSpatialAnchorProvider.WithPlatformSession(sessionActive: true);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().BeNull();
    }

    [Fact]
    public async Task R2_UnsupportedHost_ObservePose_EmitsLostNotTracking()
    {
        var provider = XrealSpatialAnchorProvider.WithPlatformSession(sessionActive: true);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    [Fact]
    public async Task R3_BlankAtomId_ObservePose_EmitsLost()
    {
        var provider = new XrealSpatialAnchorProvider();

        var sample = await provider.ObservePose("  ").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R4_UninitializedSession_StillFailsClosed()
    {
        var provider = XrealSpatialAnchorProvider.WithPlatformSession(sessionActive: false);

        (await provider.GetCurrentPose("atom-a")).Should().BeNull();
        (await provider.ObservePose("atom-a").FirstAsync()).TrackingState.Should().Be(TrackingState.Lost);
    }

    [Fact]
    public async Task R5_UnknownAtomId_GetCurrentPose_ReturnsNullNotZeroedPose()
    {
        var session = new SeamXrealNativeSession(active: true);
        var provider = new XrealSpatialAnchorProvider(session);

        (await provider.GetCurrentPose("missing")).Should().BeNull();
    }

    [Fact]
    public async Task R6_TetheredHostDisconnect_ObservePose_EmitsLostNotFrozenPose()
    {
        var session = new SeamXrealNativeSession(active: true);
        session.RegisterPose("atom-a", new XrealNativePoseFrame(
            1, 2, 3, 0, 0, 0, 1,
            XrealNativeTrackingQuality.Tracking,
            DateTimeOffset.UtcNow));

        var provider = new XrealSpatialAnchorProvider(session);
        session.SetHostDisconnected(true);

        var sample = await provider.ObservePose("atom-a").FirstAsync();

        sample.TrackingState.Should().Be(TrackingState.Lost);
        sample.Confidence.Should().Be(0);
    }

    private sealed class SeamXrealNativeSession : IXrealNativeSession
    {
        private readonly Dictionary<string, XrealNativePoseFrame> _poses = new(StringComparer.Ordinal);
        private bool _active;
        private bool _hostDisconnected;

        public SeamXrealNativeSession(bool active) => _active = active;

        public bool IsActive => _active && !_hostDisconnected;

        public bool IsHostDisconnected => _hostDisconnected;

        public void RegisterPose(string atomId, XrealNativePoseFrame frame) => _poses[atomId] = frame;

        public void SetHostDisconnected(bool disconnected) => _hostDisconnected = disconnected;

        public XrealNativePoseFrame? TryGetAnchorPose(string atomId) =>
            _poses.TryGetValue(atomId, out var frame) ? frame : null;

        public IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId)
        {
            if (_hostDisconnected)
            {
                return Observable.Return(new XrealNativePoseFrame(
                    0, 0, 0, 0, 0, 0, 1,
                    XrealNativeTrackingQuality.Lost,
                    DateTimeOffset.UtcNow));
            }

            return Observable.Empty<XrealNativePoseFrame>();
        }
    }
}
