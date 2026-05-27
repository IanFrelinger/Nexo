using FluentAssertions;
using Nexo.Core.Application.Environments;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

public sealed class NoOpEnvironmentServicesGapCoverageTests
{
    [Fact]
    public async Task NoOpMaterialIntelligenceService_returns_empty_suggestions()
    {
        var service = new NoOpMaterialIntelligenceService();

        var result = await service.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "forest",
            new Dictionary<string, string>(),
            new MapDataRequestContext("session-1")));

        result.Materials.Should().BeEmpty();
        result.Summary.Should().BeNull();
    }

    [Fact]
    public async Task NoOpVectorMapIntelligenceService_returns_unmodified_payload()
    {
        var service = new NoOpVectorMapIntelligenceService();
        var payload = new byte[] { 1, 2, 3 };
        var request = new VectorMapIntelligenceRequest(
            payload,
            "geojson",
            new MapDataRequestContext("session-1"));

        var result = await service.RefineAsync(request);

        result.RefinedPayload.ToArray().Should().Equal(1, 2, 3);
        result.FormatHint.Should().Be("geojson");
        result.WasModified.Should().BeFalse();
        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task NoOpMapVerificationService_always_passes()
    {
        var service = new NoOpMapVerificationService();

        var report = await service.VerifyAsync(new MapVerificationRequest(
            new MapDataGeographicBounds(-1, -1, 1, 1),
            TierIndex: 2,
            EnvironmentManifestId: null,
            DataBinding: null,
            VectorSamplePayload: null,
            VectorFormatHint: null,
            Context: new MapDataRequestContext("session-1")));

        report.TierIndex.Should().Be(2);
        report.PassedCoreChecks.Should().BeTrue();
        report.Issues.Should().BeEmpty();
    }
}
