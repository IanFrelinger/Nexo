using FluentAssertions;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Runtime.Platform;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// P2-S4 provider selection rejection tests.
/// </summary>
[Trait("Category", "Spatial")]
public sealed class SpatialAnchorProviderSelectorRejectionTests
{
    [Fact]
    public void R1_UnsupportedHost_ReturnsExplicitUnavailableNotCrash()
    {
        var selector = new SpatialAnchorProviderSelector();

        var resolution = selector.Resolve();

        resolution.IsUnavailable.Should().BeTrue();
        resolution.UnavailableReason.Should().Be(SpatialAnchorProviderUnavailableReason.UnsupportedHostPlatform);
        resolution.Provider.Should().BeNull();
    }

    [Fact]
    public void R2_EmptyRegistry_ReturnsNoRegisteredProvider()
    {
        var selector = new SpatialAnchorProviderSelector(registrations: Array.Empty<SpatialPlatformProviderRegistration>());

        var resolution = selector.Resolve();

        resolution.UnavailableReason.Should().Be(SpatialAnchorProviderUnavailableReason.NoRegisteredProvider);
    }

    [Fact]
    public void R3_MultipleSupportedProviders_PicksDeterministicPriority()
    {
        var calls = new List<string>();
        var registrations = new[]
        {
            CreateRegistration("xreal", supported: true, calls),
            CreateRegistration("arkit", supported: true, calls),
            CreateRegistration("visionpro", supported: true, calls)
        };

        var selector = new SpatialAnchorProviderSelector(registrations);
        var resolution = selector.Resolve();

        resolution.IsUnavailable.Should().BeFalse();
        resolution.SelectedPlatformId.Should().Be("visionpro");
        calls.Should().ContainSingle().Which.Should().Be("visionpro");
    }

    [Fact]
    public void R4_EqualPriorityTieBreaksByOrdinalPlatformId()
    {
        var registrations = new[]
        {
            CreateRegistration("beta", supported: true),
            CreateRegistration("alpha", supported: true)
        };

        var selector = new SpatialAnchorProviderSelector(
            registrations,
            priorityOrder: ["shared-tier", "shared-tier"]);

        var resolution = selector.Resolve();

        resolution.SelectedPlatformId.Should().Be("alpha");
    }

    [Fact]
    public async Task R5_ResolvedProviderStillFailsClosedOnHeadlessHost()
    {
        var selector = new SpatialAnchorProviderSelector(
            registrations: new[]
            {
                new SpatialPlatformProviderRegistration
                {
                    PlatformId = "arkit",
                    IsSupported = static () => true,
                    Factory = static () => new Nexo.Spatial.Platform.ARKit.ArKitSpatialAnchorProvider()
                }
            });

        var resolution = selector.Resolve();
        resolution.IsUnavailable.Should().BeFalse();

        var pose = await resolution.Provider!.GetCurrentPose("atom-a");
        pose.Should().BeNull("selector glue must not bypass platform fail-closed behavior");
    }

    private static SpatialPlatformProviderRegistration CreateRegistration(
        string platformId,
        bool supported,
        List<string>? calls = null) =>
        new()
        {
            PlatformId = platformId,
            IsSupported = () => supported,
            Factory = () =>
            {
                calls?.Add(platformId);
                return new FakeSpatialAnchorProvider(Array.Empty<ScriptedAtomSequence>());
            }
        };
}
