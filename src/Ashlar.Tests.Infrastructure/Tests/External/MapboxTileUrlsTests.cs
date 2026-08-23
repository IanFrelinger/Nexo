using FluentAssertions;
using Ashlar.Tests.Infrastructure.External;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.External;

/// <summary>
/// White-box tests for <see cref="MapboxTileUrls"/> — URL shape, escaping, Web Mercator bounds, and byte heuristics.
/// </summary>
public sealed class MapboxTileUrlsTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, 1)]
    [InlineData(10, 512, 300)]
    public void ValidateWebMercatorTile_AcceptsInRange(int z, int x, int y)
    {
        var act = () => MapboxTileUrls.ValidateWebMercatorTile(z, x, y);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 0, 2)]
    [InlineData(-1, 0, 0)]
    [InlineData(31, 0, 0)]
    public void ValidateWebMercatorTile_RejectsOutOfRange(int z, int x, int y)
    {
        var act = () => MapboxTileUrls.ValidateWebMercatorTile(z, x, y);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BuildRasterTileUrl_EncodesTilesetAndToken()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl("mapbox.satellite", 0, 0, 0, "pk.abc+def/");

        url.Should().StartWith("https://api.mapbox.com/v4/");
        url.Should().Contain("mapbox.satellite/0/0/0.jpg?");
        url.Should().Contain("access_token=");
        url.Should().Contain(Uri.EscapeDataString("pk.abc+def/"));
        url.Should().NotContain(" ");
    }

    [Fact]
    public void BuildRasterTileUrl_WithAt2xSuffix()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl("mapbox.satellite", 2, 1, 1, "pk.x", "@2x.jpg");

        url.Should().Contain("/2/1/1@2x.jpg?");
        url.Should().NotContain(".@2x");
    }

    [Fact]
    public void BuildRasterTileUrl_NormalizesSuffixWithoutLeadingDot()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl("mapbox.satellite", 0, 0, 0, "pk.x", "jpg");

        url.Should().Contain("/0/0/0.jpg?");
    }

    [Fact]
    public void BuildVectorTileUrl_EncodesTilesetWithSlash()
    {
        var url = MapboxTileUrls.BuildVectorTileUrl("user.custom-v1", 3, 2, 4, "pk.token");

        url.Should().Contain("/v4/user.custom-v1/3/2/4.vector.pbf?");
        url.Should().Contain("access_token=pk.token");
    }

    [Fact]
    public void BuildRasterTileUrl_EncodesSlashInTilesetId()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl("user.foo/bar", 0, 0, 0, "pk.x");

        url.Should().Contain(Uri.EscapeDataString("user.foo/bar"));
    }

    [Fact]
    public void BuildRasterTileUrl_EmptyTileset_Throws()
    {
        var act = () => MapboxTileUrls.BuildRasterTileUrl("  ", 0, 0, 0, "pk.x");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildRasterTileUrl_EmptyToken_Throws()
    {
        var act = () => MapboxTileUrls.BuildRasterTileUrl("mapbox.satellite", 0, 0, 0, "  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsJpegImage_DetectsSoi()
    {
        MapboxTileUrls.IsJpegImage(new byte[] { 0xFF, 0xD8, 0xFF }).Should().BeTrue();
        MapboxTileUrls.IsJpegImage(new byte[] { 0x1F, 0x8B }).Should().BeFalse();
    }

    [Fact]
    public void IsGzipPayload_DetectsMagic()
    {
        MapboxTileUrls.IsGzipPayload(new byte[] { 0x1F, 0x8B, 0x08 }).Should().BeTrue();
        MapboxTileUrls.IsGzipPayload(new byte[] { 0xFF, 0xD8 }).Should().BeFalse();
    }

    [Theory]
    [InlineData("application/x-protobuf")]
    [InlineData("application/vnd.mapbox-vector-tile")]
    [InlineData("application/octet-stream")]
    [InlineData("application/gzip")]
    [InlineData(null)]
    [InlineData("")]
    public void IsLikelyVectorTileContentType_AcceptsExpected(string? media)
    {
        MapboxTileUrls.IsLikelyVectorTileContentType(media).Should().BeTrue();
    }

    [Fact]
    public void IsLikelyVectorTileContentType_RejectsHtml()
    {
        MapboxTileUrls.IsLikelyVectorTileContentType("text/html").Should().BeFalse();
    }
}
