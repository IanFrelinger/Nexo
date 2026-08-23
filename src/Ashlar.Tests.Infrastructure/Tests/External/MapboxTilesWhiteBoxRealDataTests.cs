using System.Net;
using FluentAssertions;
using Ashlar.Tests.Infrastructure.External;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.External;

/// <summary>
/// White-box integration: tile indices from <see cref="MapboxWebMercatorTileMath"/>, URLs from
/// <see cref="MapboxTileUrls"/>, response checks from <see cref="MapboxTileResponseValidators"/>,
/// against the live Mapbox API when <c>ASHLAR_TEST_MAPBOX_TILES=1</c> and <c>MAPBOX_ACCESS_TOKEN</c> are set
/// (reported as Skipped otherwise, see <see cref="OptInFactAttribute"/>).
/// </summary>
[Trait("Category", "External")]
[Trait("Category", "MapboxRealData")]
public sealed class MapboxTilesWhiteBoxRealDataTests
{
    private const string EnableEnv = "ASHLAR_TEST_MAPBOX_TILES";
    private const string TokenEnv = "MAPBOX_ACCESS_TOKEN";
    private const string RasterTilesetEnv = "MAPBOX_TILESET_ID";
    private const string VectorTilesetEnv = "MAPBOX_VECTOR_TILESET_ID";
    private const string Dependency = "Mapbox Tiles API (public internet)";

    [OptInFact(EnableEnv, Dependency, RequiredEnvironmentVariables = new[] { TokenEnv }, Timeout = 90000)]
    public async Task RasterTile_ComputedTileIndexFromGeography_MatchesValidators()
    {
        var token = Environment.GetEnvironmentVariable(TokenEnv)!; // non-empty: enforced by RequiredEnvironmentVariables

        var tileset = Environment.GetEnvironmentVariable(RasterTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = MapboxTileUrls.DefaultRasterTilesetId;

        var (x, y) = MapboxWebMercatorTileMath.TileIndexFromLatLon(37.7749, -122.4194, 10);
        x.Should().Be(163);
        y.Should().Be(395);

        var url = MapboxTileUrls.BuildRasterTileUrl(tileset, 10, x, y, token);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        MapboxTileResponseValidators.AssertRasterTileSuccess(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            bytes);
    }

    [OptInFact(EnableEnv, Dependency, RequiredEnvironmentVariables = new[] { TokenEnv }, Timeout = 90000)]
    public async Task VectorTile_ComputedTileIndexFromGeography_MatchesValidators()
    {
        var token = Environment.GetEnvironmentVariable(TokenEnv)!; // non-empty: enforced by RequiredEnvironmentVariables

        var tileset = Environment.GetEnvironmentVariable(VectorTilesetEnv)?.Trim();
        if (string.IsNullOrEmpty(tileset))
            tileset = MapboxTileUrls.DefaultVectorTilesetId;

        var (x, y) = MapboxWebMercatorTileMath.TileIndexFromLatLon(37.7749, -122.4194, 10);

        var url = MapboxTileUrls.BuildVectorTileUrl(tileset, 10, x, y, token);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(url, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        MapboxTileResponseValidators.AssertVectorTileSuccess(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            bytes);
    }
}
