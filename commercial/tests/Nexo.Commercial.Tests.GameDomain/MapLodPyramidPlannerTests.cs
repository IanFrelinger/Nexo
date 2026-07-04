using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for map lod pyramid planner.</summary>
public sealed class MapLodPyramidPlannerTests
{
    [Fact]
    public void Build_EmptyLod_ReturnsSingleTierAtFinestZoom()
    {
        var tiers = MapLodPyramidPlanner.Build([], 14);
        tiers.Should().HaveCount(1);
        tiers[0].Zoom.Should().Be(14);
        tiers[0].ApproxAreaMultiplierVsFinest.Should().Be(1.0);
    }

    [Fact]
    public void Build_VoxelPack_MatchesSteppedZoom()
    {
        var lod =
            new[]
            {
                new LodLevel(0, 1.0),
                new LodLevel(1, 0.5),
                new LodLevel(2, 0.25)
            };
        var tiers = MapLodPyramidPlanner.Build(lod, 14);
        tiers.Should().HaveCount(3);
        tiers[0].Zoom.Should().Be(14);
        tiers[1].Zoom.Should().Be(13);
        tiers[2].Zoom.Should().Be(12);
        tiers[2].ApproxAreaMultiplierVsFinest.Should().Be(Math.Pow(4, 2));
    }

    [Fact]
    public void Build_RejectsFinestZoomOutOfRange()
    {
        var act = () => MapLodPyramidPlanner.Build([new LodLevel(0, 1)], 23);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
