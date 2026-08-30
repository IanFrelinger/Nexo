using FluentAssertions;
using Ashlar.Spatial.Platform.XREAL;
using Ashlar.Spatial.Platform.XREAL.Interop;
using System.Reactive.Linq;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S2 XREAL native-session fail-closed tests (headless CI is never an Android
/// NRSDK host, so both session implementations must refuse to fabricate poses).
/// The live NRSDK branches (bridge pose lookup, disconnect frame synthesis) are
/// reachable only on Android and are deliberately not simulated here.
/// </summary>
[Trait("Category", "Spatial")]
public sealed class XrealNativeSessionFailClosedTests
{
    [Fact]
    public void U1_UninitializedSession_SingletonFailsClosed()
    {
        var session = UninitializedXrealNativeSession.Instance;

        UninitializedXrealNativeSession.Instance.Should().BeSameAs(session);
        session.IsActive.Should().BeFalse();
        session.IsHostDisconnected.Should().BeFalse();
        session.TryGetAnchorPose("atom-a").Should().BeNull();
    }

    [Fact]
    public async Task U2_UninitializedSession_ObserveAnchorPose_CompletesEmpty()
    {
        var frames = await UninitializedXrealNativeSession.Instance
            .ObserveAnchorPose("atom-a")
            .ToList();

        frames.Should().BeEmpty();
    }

    [Fact]
    public void P1_PlatformSession_OnHeadlessHost_NeverActivates()
    {
        XrealSpatialAvailability.IsSupported().Should().BeFalse("headless CI is never an Android NRSDK host");

        var session = new PlatformXrealNativeSession();
        session.IsActive.Should().BeFalse();
        session.IsHostDisconnected.Should().BeFalse();

        session.SetActive(true);
        session.IsActive.Should().BeFalse("NRSDK availability gates activation on unsupported hosts");
    }

    [Fact]
    public void P2_PlatformSession_TracksHostDisconnectFlag()
    {
        var session = new PlatformXrealNativeSession(isActive: true);

        session.SetHostDisconnected(true);
        session.IsHostDisconnected.Should().BeTrue();
        session.IsActive.Should().BeFalse("a disconnected host can never be active");

        session.SetHostDisconnected(false);
        session.IsHostDisconnected.Should().BeFalse();
    }

    [Fact]
    public void P3_PlatformSession_TryGetAnchorPose_FailsClosed()
    {
        var session = new PlatformXrealNativeSession(isActive: true);

        session.TryGetAnchorPose("atom-a").Should().BeNull("unsupported hosts must not fabricate poses");
        session.TryGetAnchorPose("   ").Should().BeNull("blank atom ids are rejected");
        new PlatformXrealNativeSession().TryGetAnchorPose("atom-a").Should().BeNull("inactive sessions have no poses");
    }

    [Fact]
    public async Task P4_PlatformSession_ObserveAnchorPose_FailsClosedEmpty()
    {
        var inactive = new PlatformXrealNativeSession();
        (await inactive.ObserveAnchorPose("atom-a").ToList()).Should().BeEmpty();

        var activeButUnsupported = new PlatformXrealNativeSession(isActive: true);
        (await activeButUnsupported.ObserveAnchorPose("atom-a").ToList()).Should().BeEmpty();
        (await activeButUnsupported.ObserveAnchorPose(" ").ToList()).Should().BeEmpty();
    }
}
