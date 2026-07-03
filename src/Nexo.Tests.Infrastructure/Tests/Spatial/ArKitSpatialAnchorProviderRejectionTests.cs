using FluentAssertions;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Platform.ARKit;
using System.Reactive.Linq;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2 ARKit provider rejection tests (headless CI — no device SDK).
/// </summary>
[Trait("Category", "Spatial")]
public sealed class ArKitSpatialAnchorProviderRejectionTests
{
    [Fact]
    public async Task R1_NonIosHost_GetCurrentPose_ReturnsNullNotFabricatedPose()
    {
        ArKitSpatialAvailability.IsSupported().Should().BeFalse("headless CI is never iOS");

        var provider = new ArKitSpatialAnchorProvider(sessionReady: true);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().BeNull();
    }

    [Fact]
    public async Task R2_NonIosHost_ObservePose_EmitsLostNotTracking()
    {
        var provider = new ArKitSpatialAnchorProvider(sessionReady: true);

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
        var provider = new ArKitSpatialAnchorProvider(sessionReady: false);
        var pose = await provider.GetCurrentPose("atom-a");

        pose.Should().BeNull();
        var sample = await provider.ObservePose("atom-a").FirstAsync();
        sample.TrackingState.Should().Be(TrackingState.Lost);
    }
}
