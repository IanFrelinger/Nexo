using FluentAssertions;
using Nexo.GameDomain.Maps;
using Xunit;

namespace Nexo.Tests.GameDomain;

public class MapboxTileUrlsTests
{
    [Fact]
    public void BuildRasterTileUrl_EncodesToken()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl("mapbox.satellite", 0, 0, 0, "pk.a+b");
        url.Should().Contain("access_token=");
        url.Should().Contain(Uri.EscapeDataString("pk.a+b"));
    }

    [Fact]
    public void BuildVectorTileUrl_UsesVectorSuffix()
    {
        var url = MapboxTileUrls.BuildVectorTileUrl("mapbox.mapbox-streets-v8", 1, 0, 0, "pk.x");
        url.Should().Contain("/1/0/0.vector.pbf?");
    }
}
