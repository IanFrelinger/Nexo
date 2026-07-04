using FluentAssertions;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for vector tile url parser.</summary>
public sealed class VectorTileUrlParserTests
{
    [Theory]
    [InlineData("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/14/4823/6150.vector.pbf?access_token=x", 14, 4823, 6150)]
    [InlineData("https://example.com/tiles/10/1/2.mvt", 10, 1, 2)]
    [InlineData("https://example.com/foo?z=12&x=100&y=200", 12, 100, 200)]
    public void TryParseTile_ParsesCommonLayouts(string url, int ez, int ex, int ey)
    {
        VectorTileUrlParser.TryParseTile(url, out var z, out var x, out var y).Should().BeTrue();
        z.Should().Be(ez);
        x.Should().Be(ex);
        y.Should().Be(ey);
    }

    [Fact]
    public void TryParseTile_RejectsInvalidZoom()
    {
        VectorTileUrlParser.TryParseTile("https://example.com/99/1/1/foo", out _, out _, out _).Should().BeFalse();
    }
}
