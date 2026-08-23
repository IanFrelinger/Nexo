using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for map tile cache key.</summary>
public sealed class MapTileCacheKeyTests
{
    [Fact]
    public void Create_SanitizesSegments()
    {
        var k = MapTileCacheKey.Create("my pack!", "map/box", 10, 3, 4);
        k.AestheticId.Should().Be("my_pack_");
        k.ProviderId.Should().Be("map_box");
        k.RelativePath.Should().Contain("10/3/4.bin");
    }
}
