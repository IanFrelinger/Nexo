using System.Net;
using FluentAssertions;
using Ashlar.Tests.Infrastructure.External;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.External;

/// <summary>
/// Optional black-box checks against Mapbox’s Raster and Vector Tiles APIs (HTTPS, no SDK).
/// <para>
/// Enable with <c>ASHLAR_TEST_MAPBOX_TILES=1</c> and <c>MAPBOX_ACCESS_TOKEN</c> set (never commit tokens).
/// Raster: default tileset <c>mapbox.satellite</c> (<c>MAPBOX_TILESET_ID</c>).
/// Vector: default tileset <c>mapbox.mapbox-streets-v8</c> (<c>MAPBOX_VECTOR_TILESET_ID</c>), extension <c>.vector.pbf</c>.
/// </para>
/// Every test here reaches the public internet, so every test gates on <c>ASHLAR_TEST_MAPBOX_TILES=1</c>
/// (the valid-token tests additionally require the token) via <see cref="OptInFactAttribute"/> and is
/// reported as Skipped otherwise, keeping CI and offline runs green without counting as Passed. Before
/// this gate covered the invalid-token tests they ran unconditionally and turned the kernel-coverage
/// badge red on a transient egress failure (2026-08-15).
/// URL construction and response rules are white-box tested in <see cref="MapboxTileUrlsTests"/> and
/// <see cref="MapboxTileResponseValidatorsTests"/>. Geography-driven URLs with real Mapbox responses are in
/// <see cref="MapboxTilesWhiteBoxRealDataTests"/> (same env gate).
/// </summary>
[Trait("Category", "External")]
public sealed class MapboxTilesBlackBoxTests
{
    private const string EnableEnv = "ASHLAR_TEST_MAPBOX_TILES";
    private const string TokenEnv = "MAPBOX_ACCESS_TOKEN";
    private const string RasterTilesetEnv = "MAPBOX_TILESET_ID";
    private const string VectorTilesetEnv = "MAPBOX_VECTOR_TILESET_ID";
    private const string Dependency = "Mapbox Tiles API (public internet)";

    [OptInFact(EnableEnv, Dependency, RequiredEnvironmentVariables = new[] { TokenEnv }, Timeout = 60000)]
    public async Task RasterTile_ValidToken_ReturnsRasterBytes()
    {
        var token = Environment.GetEnvironmentVariable(TokenEnv)!; // non-empty: enforced by RequiredEnvironmentVariables

        var tileset = Environment.GetEnvironmentVariable(RasterTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = MapboxTileUrls.DefaultRasterTilesetId;

        var url = MapboxTileUrls.BuildRasterTileUrl(tileset, 0, 0, 0, token);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        MapboxTileResponseValidators.AssertRasterTileSuccess(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            bytes);
    }

    [OptInFact(EnableEnv, Dependency, RequiredEnvironmentVariables = new[] { TokenEnv }, Timeout = 60000)]
    public async Task VectorTile_ValidToken_ReturnsTileBytes()
    {
        var token = Environment.GetEnvironmentVariable(TokenEnv)!; // non-empty: enforced by RequiredEnvironmentVariables

        var tileset = Environment.GetEnvironmentVariable(VectorTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = MapboxTileUrls.DefaultVectorTilesetId;

        var url = MapboxTileUrls.BuildVectorTileUrl(tileset, 0, 0, 0, token);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        MapboxTileResponseValidators.AssertVectorTileSuccess(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            bytes);
    }

    // No token needed (the point is the 401/403), but the request still needs egress.
    [OptInFact(EnableEnv, Dependency, Timeout = 30000)]
    public async Task VectorTile_InvalidToken_IsUnauthorized()
    {
        var url = MapboxTileUrls.BuildVectorTileUrl(
            MapboxTileUrls.DefaultVectorTilesetId,
            0,
            0,
            0,
            "pk.THIS_IS_NOT_A_REAL_MAPBOX_TOKEN");

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    // No token needed (the point is the 401/403), but the request still needs egress.
    [OptInFact(EnableEnv, Dependency, Timeout = 30000)]
    public async Task RasterTile_InvalidToken_IsUnauthorized()
    {
        var url = MapboxTileUrls.BuildRasterTileUrl(
            MapboxTileUrls.DefaultRasterTilesetId,
            0,
            0,
            0,
            "pk.THIS_IS_NOT_A_REAL_MAPBOX_TOKEN");

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
