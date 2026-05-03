using System.Net;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.External;

/// <summary>
/// Optional black-box checks against Mapbox’s Raster and Vector Tiles APIs (HTTPS, no SDK).
/// <para>
/// Enable with <c>NEXO_TEST_MAPBOX_TILES=1</c> and <c>MAPBOX_ACCESS_TOKEN</c> set (never commit tokens).
/// Raster: default tileset <c>mapbox.satellite</c> (<c>MAPBOX_TILESET_ID</c>).
/// Vector: default tileset <c>mapbox.mapbox-streets-v8</c> (<c>MAPBOX_VECTOR_TILESET_ID</c>), extension <c>.vector.pbf</c>.
/// </para>
/// When disabled or token missing, tests return immediately so CI and offline runs stay green.
/// </summary>
[Trait("Category", "External")]
public sealed class MapboxTilesBlackBoxTests
{
    private const string EnableEnv = "NEXO_TEST_MAPBOX_TILES";
    private const string TokenEnv = "MAPBOX_ACCESS_TOKEN";
    private const string RasterTilesetEnv = "MAPBOX_TILESET_ID";
    private const string VectorTilesetEnv = "MAPBOX_VECTOR_TILESET_ID";

    private static bool IsExplicitlyEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable(EnableEnv), "1", StringComparison.OrdinalIgnoreCase);

    [Fact(Timeout = 60000)]
    public async Task RasterTile_ValidToken_ReturnsRasterBytes()
    {
        if (!IsExplicitlyEnabled())
            return;

        var token = Environment.GetEnvironmentVariable(TokenEnv);
        if (string.IsNullOrWhiteSpace(token))
            return;

        var tileset = Environment.GetEnvironmentVariable(RasterTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = "mapbox.satellite";

        // Zoom 0 single world tile — minimal black-box probe (Mapbox Raster Tiles API v4).
        var url =
            $"https://api.mapbox.com/v4/{Uri.EscapeDataString(tileset)}/0/0/0.jpg?access_token={Uri.EscapeDataString(token.Trim())}";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {tileset}/0/0/0.jpg should succeed with a valid token");
        response.Content.Headers.ContentType?.MediaType.Should().StartWith("image/", "raster tiles return an image content type");

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        bytes.Should().NotBeEmpty();
        // JPEG SOI
        bytes.Length.Should().BeGreaterThan(100);
        bytes[0].Should().Be(0xFF);
        bytes[1].Should().Be(0xD8);
    }

    [Fact(Timeout = 60000)]
    public async Task VectorTile_ValidToken_ReturnsTileBytes()
    {
        if (!IsExplicitlyEnabled())
            return;

        var token = Environment.GetEnvironmentVariable(TokenEnv);
        if (string.IsNullOrWhiteSpace(token))
            return;

        var tileset = Environment.GetEnvironmentVariable(VectorTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = "mapbox.mapbox-streets-v8";

        // Vector Tiles API — MVT protobuf (.vector.pbf). Zoom 0 world tile.
        var url =
            $"https://api.mapbox.com/v4/{Uri.EscapeDataString(tileset)}/0/0/0.vector.pbf?access_token={Uri.EscapeDataString(token.Trim())}";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {tileset}/0/0/0.vector.pbf should succeed with a valid token");

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(20);

        var media = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!string.IsNullOrEmpty(media))
        {
            var ok =
                media.Contains("protobuf", StringComparison.OrdinalIgnoreCase) ||
                media.Contains("octet-stream", StringComparison.OrdinalIgnoreCase) ||
                media.Contains("mapbox-vector-tile", StringComparison.OrdinalIgnoreCase) ||
                media.Contains("gzip", StringComparison.OrdinalIgnoreCase) ||
                media.Contains("application/x-protobuf", StringComparison.OrdinalIgnoreCase);
            ok.Should().BeTrue($"unexpected Content-Type for vector tile: {media}");
        }

        // Payload is often gzip (starts with 1F 8B); otherwise raw MVT protobuf.
    }

    [Fact(Timeout = 30000)]
    public async Task VectorTile_InvalidToken_IsUnauthorized()
    {
        var url =
            "https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/0/0/0.vector.pbf?access_token=pk.THIS_IS_NOT_A_REAL_MAPBOX_TOKEN";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact(Timeout = 30000)]
    public async Task RasterTile_InvalidToken_IsUnauthorized()
    {
        var url = "https://api.mapbox.com/v4/mapbox.satellite/0/0/0.jpg?access_token=pk.THIS_IS_NOT_A_REAL_MAPBOX_TOKEN";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
