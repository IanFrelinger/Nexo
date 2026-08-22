using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for map tile disk cache.</summary>
public sealed class MapTileDiskCacheTests
{
    [Fact]
    public async Task WriteAsync_RoundTrips()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-cache-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cache = new MapTileDiskCache(root);
            var key = MapTileCacheKey.Create("voxel", "mapbox", 14, 1, 2);
            var data = new byte[] { 9, 8, 7 };
            await cache.WriteAsync(key, data);
            var read = await cache.TryReadAsync(key);
            read.Should().Equal(data);
            cache.GetFullPath(key).Should().StartWith(root);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // temp cleanup best-effort
            }
        }
    }
}
