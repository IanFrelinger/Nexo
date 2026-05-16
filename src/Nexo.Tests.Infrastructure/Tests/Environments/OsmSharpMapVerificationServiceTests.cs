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
}
