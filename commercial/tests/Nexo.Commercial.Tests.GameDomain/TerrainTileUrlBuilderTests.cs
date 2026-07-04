using FluentAssertions;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for terrain tile url builder.</summary>
public sealed class TerrainTileUrlBuilderTests
{
    [Fact]
    public void MapboxTerrainRgbTileUrl_ContainsZxyAndToken()
    {
        var u = TerrainTileUrlBuilder.MapboxTerrainRgbTileUrl(14, 12_345, 67_890, "pk.abc+def");
        u.Should().StartWith("https://api.mapbox.com/v4/mapbox.terrain-rgb/14/12345/67890.pngraw?access_token=");
        u.Should().Contain(Uri.EscapeDataString("pk.abc+def"));
    }
}
