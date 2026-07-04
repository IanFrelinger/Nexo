using FluentAssertions;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for tier classification.</summary>
public sealed class TierClassificationTests
{
    [Theory]
    [InlineData(4L, 0L, NodeTier.Nano)]
    [InlineData(12L, 0L, NodeTier.Micro)]
    [InlineData(32L, 0L, NodeTier.Standard)]
    [InlineData(64L, 0L, NodeTier.Core)]
    [InlineData(8L, 12L, NodeTier.Standard)]
    [InlineData(8L, 24L, NodeTier.Core)]
    public void ClassifyTier_MapsRepresentativeHardware(
        long ramGiB,
        long vramGiB,
        NodeTier expected)
    {
        var profile = new NodeProfile
        {
            TotalRAMBytes = ramGiB * 1024 * 1024 * 1024,
            TotalVRAMBytes = vramGiB * 1024 * 1024 * 1024
        };

        var tier = EnvironmentHardwareProfiler.ClassifyTier(profile);
        tier.Should().Be(expected);
    }
}
