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

/// <summary>Tests for model backed material intelligence service gap coverage.</summary>
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
