using FluentAssertions;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for vector tile url builder.</summary>
public sealed class VectorTileUrlBuilderTests
{
    [Fact]
    public void MapboxVectorTileUrl_IncludesEncodedToken()
    {
        var url = VectorTileUrlBuilder.MapboxVectorTileUrl(
            "mapbox.mapbox-streets-v8",
            14,
            4823,
            6150,
            "pk.token&with=special");
        url.Should().StartWith("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/14/4823/6150.vector.pbf?");
        url.Should().Contain("access_token=pk.token%26with%3Dspecial");
    }
}
