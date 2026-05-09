using FluentAssertions;
using Nexo.Tests.Infrastructure.External;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.External;

public sealed class MapboxWebMercatorTileMathTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 1, 1)]
    public void TileIndexFromLatLon_OriginAndZ1(double lat, double lon, int z, int expectX, int expectY)
    {
        var (x, y) = MapboxWebMercatorTileMath.TileIndexFromLatLon(lat, lon, z);
        x.Should().Be(expectX);
        y.Should().Be(expectY);
    }

    [Fact]
    public void TileIndexFromLatLon_SanFranciscoZoom10_MatchesReferenceTile()
    {
        // Reference: Web Mercator XYZ at z=10 for approx downtown SF.
        var (x, y) = MapboxWebMercatorTileMath.TileIndexFromLatLon(37.7749, -122.4194, 10);
        x.Should().Be(163);
        y.Should().Be(395);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void TileIndexFromLatLon_InvalidZoom_Throws(int z)
    {
        var act = () => MapboxWebMercatorTileMath.TileIndexFromLatLon(0, 0, z);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TileIndexFromLatLon_DateLine_Clamped()
    {
        var (x, _) = MapboxWebMercatorTileMath.TileIndexFromLatLon(0, 180, 2);
        x.Should().Be(3);
    }
}
