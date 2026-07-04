using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

/// <summary>Tests for osm sharp map verification service gap coverage.</summary>
public sealed class OsmSharpMapVerificationServiceGapCoverageTests
{
    [Fact]
    public async Task VerifyAsync_without_payload_emits_info_and_passes_core_checks()
    {
        var service = new OsmSharpMapVerificationService();
        var report = await service.VerifyAsync(new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 1,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: null,
            VectorFormatHint: null,
            Context: new MapDataRequestContext("session-1")));

        report.PassedCoreChecks.Should().BeTrue();
        report.Issues.Should().ContainSingle(i =>
            i.Message.Contains("No vector sample payload", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VerifyAsync_skips_non_osm_payload_with_info_issue()
    {
        var service = new OsmSharpMapVerificationService();
        var report = await service.VerifyAsync(new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 1,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: Encoding.UTF8.GetBytes("""{"type":"FeatureCollection"}"""),
            VectorFormatHint: "geojson",
            Context: new MapDataRequestContext("session-2")));

        report.PassedCoreChecks.Should().BeTrue();
        report.Issues.Should().ContainSingle(i =>
            i.Message.Contains("OsmSharp checks skipped", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VerifyAsync_promotes_parent_tier_errors_to_warnings()
    {
        var parent = new MapVerificationReport(
            0,
            [
                new MapVerificationIssue(
                    MapVerificationCategories.Topology,
                    MapVerificationSeverity.Error,
                    "parent topology error",
                    new Dictionary<string, string>()),
            ],
            PassedCoreChecks: false);

        var service = new OsmSharpMapVerificationService();
        var report = await service.VerifyAsync(new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 1,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: null,
            VectorFormatHint: null,
            Context: new MapDataRequestContext("session-3"),
            ParentTierReport: parent));

        report.Issues.Should().ContainSingle(i =>
            i.Severity == MapVerificationSeverity.Warning &&
            i.Message.Contains("Parent tier reported error", StringComparison.Ordinal));
    }
}
