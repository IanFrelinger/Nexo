using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for heuristic map verification service.</summary>
public sealed class HeuristicMapVerificationServiceTests
{
    private readonly HeuristicMapVerificationService _sut = new();

    [Fact]
    public async Task VerifyAsync_GeoJson_AddsFeatureCountInfo()
    {
        var insp = new VectorPayloadInspection("json_vector", "", []);
        var parse = new VectorMapParseSummary(
            "geojson",
            "GeoJSON: 1 feature(s) — 1 Point",
            []);
        var r = await _sut.VerifyAsync(insp, parse);
        r.Issues.Should().Contain(i => i.Code == "geojson_feature_count");
        r.Summary.Should().Contain("geojson_feature_count");
    }

    [Fact]
    public async Task VerifyAsync_Mvt_WithRoadKeyword_AddsTransportHint()
    {
        var insp = new VectorPayloadInspection("mapbox_vector_tile_or_binary", "", []);
        var parse = new VectorMapParseSummary(
            "mvt",
            "MVT: 5 features",
            ["layers: road:3, building:2"]);
        var r = await _sut.VerifyAsync(insp, parse);
        r.Issues.Should().Contain(i => i.Code == "mvt_transport_layer_hint");
    }

    [Fact]
    public async Task VerifyAsync_ParseError_AddsWarning()
    {
        var insp = new VectorPayloadInspection("unknown_binary", "", []);
        var parse = new VectorMapParseSummary("error", "broken", []);
        var r = await _sut.VerifyAsync(insp, parse);
        r.Issues.Should().Contain(i => i.Code == "parse_failed");
    }
}
