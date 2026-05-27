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

public sealed class ModelBackedMaterialIntelligenceServiceGapCoverageTests
{
    [Fact]
    public async Task Uses_heuristic_materials_when_model_throws()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("model unavailable"));

        var service = new ModelBackedMaterialIntelligenceService(model.Object);
        var result = await service.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "coastal",
            new Dictionary<string, string> { ["highway"] = "primary" },
            new MapDataRequestContext("session-1")));

        result.Materials.Should().ContainSingle(m => m.Id == "mat_road_asphalt");
        result.Summary.Should().StartWith("model_error_fallback_heuristic:");
    }

    [Fact]
    public async Task Parses_markdown_wrapped_json_from_model_output()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                Here are materials:
                [{"id":"wrapped","name":"Wrapped","colorHex":"#AABBCC","metallic":0.2,"smoothness":0.4,"renderMode":"Opaque"}]
                """));

        var service = new ModelBackedMaterialIntelligenceService(model.Object);
        var result = await service.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "urban",
            new Dictionary<string, string>(),
            new MapDataRequestContext("session-2"),
            MaxMaterials: 4));

        result.Materials.Should().ContainSingle(m => m.Id == "wrapped");
        result.Summary.Should().Be("model_json");
    }

    [Fact]
    public async Task Heuristic_adds_water_material_for_natural_water_tags()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("[]"));

        var service = new ModelBackedMaterialIntelligenceService(model.Object);
        var result = await service.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "lake",
            new Dictionary<string, string> { ["natural"] = "water" },
            new MapDataRequestContext("session-3")));

        result.Materials.Should().ContainSingle(m => m.Id == "mat_water");
        result.Summary.Should().Be("model_returned_empty_fallback_heuristic");
    }
}

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

public sealed class MapDataServiceCollectionExtensionsGapCoverageTests
{
    [Fact]
    public void AddMapDataProviderRouting_registers_no_op_defaults()
    {
        var services = new ServiceCollection();
        services.AddMapDataProviderRouting();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMapDataProviderRouter>().Should().NotBeNull();
        provider.GetRequiredService<IVectorMapIntelligenceService>().Should().BeOfType<NoOpVectorMapIntelligenceService>();
        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<NoOpMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<NoOpMapVerificationService>();
    }

    [Fact]
    public void ReplaceMapIntelligenceWithModelBackedDefaults_swaps_to_model_and_osm_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IModel>());
        services.AddMapDataProviderRouting();
        services.ReplaceMapIntelligenceWithModelBackedDefaults();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IVectorMapIntelligenceService>().Should().BeOfType<ModelBackedVectorMapIntelligenceService>();
        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<ModelBackedMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<OsmSharpMapVerificationService>();
    }

    [Fact]
    public void ReplaceMapVerificationWithOsmSharp_swaps_only_verification_service()
    {
        var services = new ServiceCollection();
        services.AddMapDataProviderRouting();
        services.ReplaceMapVerificationWithOsmSharp();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMaterialIntelligenceService>().Should().BeOfType<NoOpMaterialIntelligenceService>();
        provider.GetRequiredService<IMapVerificationService>().Should().BeOfType<OsmSharpMapVerificationService>();
    }
}
