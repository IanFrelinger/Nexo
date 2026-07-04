using FluentAssertions;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for web mercator tile math.</summary>
public sealed class WebMercatorTileMathTests
{
    [Fact]
    public void LonLatToTileXY_RoundtripsZoomAndTileBounds()
    {
        var lon = -122.4194;
        var lat = 37.7749;
        var z = 14;
        var (x, y) = WebMercatorTileMath.LonLatToTileXY(lon, lat, z);
        var (nwLon, nwLat) = WebMercatorTileMath.TileXYToNwLonLat(x, y, z);
        var (x2, y2) = WebMercatorTileMath.LonLatToTileXY(
            nwLon + 0.0001,
            nwLat - 0.0001,
            z);
        x2.Should().Be(x);
        y2.Should().Be(y);
    }

    [Fact]
    public void LonLatToTileXY_AtZoom0_IsSingleWorldTile()
    {
        var (x, y) = WebMercatorTileMath.LonLatToTileXY(0, 0, 0);
        x.Should().Be(0);
        y.Should().Be(0);
    }
}
