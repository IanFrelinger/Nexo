using System.Text;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

public sealed class OsmSharpMapVerificationServiceTests
{
    private readonly OsmSharpMapVerificationService _svc = new();

    [Fact]
    public async Task Closed_water_polygon_produces_no_water_warning()
    {
        var xml = """
            <?xml version="1.0"?>
            <osm version="0.6">
              <node id="1" lat="0" lon="0"/>
              <node id="2" lat="0.001" lon="0"/>
              <node id="3" lat="0.001" lon="0.001"/>
              <node id="4" lat="0" lon="0.001"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="natural" v="water"/>
              </way>
            </osm>
            """;
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: Encoding.UTF8.GetBytes(xml),
            VectorFormatHint: "osm-xml",
            Context: new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().NotContain(i =>
            i.Category == MapVerificationCategories.Water &&
            i.Message.Contains("closed polygon", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Open_water_polygon_warns()
    {
        var xml = """
            <?xml version="1.0"?>
            <osm version="0.6">
              <node id="1" lat="0" lon="0"/>
              <node id="2" lat="0.001" lon="0"/>
              <node id="3" lat="0.001" lon="0.001"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/>
                <tag k="natural" v="water"/>
              </way>
            </osm>
            """;
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            null,
            null,
            Encoding.UTF8.GetBytes(xml),
            "osm-xml",
            new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().Contain(i =>
            i.Category == MapVerificationCategories.Water &&
            i.Severity == MapVerificationSeverity.Warning);
    }

    [Fact]
    public async Task Parent_tier_errors_surface_as_warnings_at_child()
    {
        var parent = new MapVerificationReport(0,
            [new MapVerificationIssue(MapVerificationCategories.Road, MapVerificationSeverity.Error, "broken")],
            PassedCoreChecks: false);

        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 1,
            null,
            null,
            null,
            null,
            new MapDataRequestContext(),
            ParentTierReport: parent);

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().Contain(i =>
            i.Message.Contains("Parent tier", StringComparison.OrdinalIgnoreCase) &&
            i.Severity == MapVerificationSeverity.Warning);
    }

    [Fact]
    public async Task Missing_vector_payload_emits_info_issue_only()
    {
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            null,
            null,
            null,
            null,
            new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().ContainSingle(i =>
            i.Category == MapVerificationCategories.Topology &&
            i.Severity == MapVerificationSeverity.Info &&
            i.Message.Contains("No vector sample payload"));
    }

    [Fact]
    public async Task Non_osm_format_hint_skips_geometry_checks()
    {
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            null,
            null,
            Encoding.UTF8.GetBytes("plain text"),
            "text/plain",
            new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().Contain(i => i.Message.Contains("OsmSharp checks skipped"));
    }

    [Fact]
    public async Task Invalid_osm_xml_emits_parse_warning()
    {
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 0,
            null,
            null,
            Encoding.UTF8.GetBytes("<osm><broken"),
            "osm-xml",
            new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().Contain(i =>
            i.Category == MapVerificationCategories.Topology &&
            i.Severity == MapVerificationSeverity.Warning &&
            i.Message.Contains("OSM parse failed"));
    }

    [Fact]
    public async Task Building_and_road_issues_are_reported()
    {
        var xml = """
            <?xml version="1.0"?>
            <osm version="0.6">
              <node id="1" lat="0" lon="0"/>
              <node id="2" lat="0.001" lon="0"/>
              <node id="3" lat="0.001" lon="0.001"/>
              <way id="20">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="primary"/>
              </way>
              <way id="21">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/>
                <tag k="building" v="yes"/>
              </way>
            </osm>
            """;
        var req = new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 2,
            null,
            null,
            Encoding.UTF8.GetBytes(xml),
            "osm-xml",
            new MapDataRequestContext());

        var report = await _svc.VerifyAsync(req, default);

        report.Issues.Should().Contain(i => i.Category == MapVerificationCategories.Road);
        report.Issues.Should().Contain(i => i.Category == MapVerificationCategories.Building);
    }
}
