using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Environments;
using Ashlar.Core.Application.Environments.Ports;
using Ashlar.Infrastructure.Environments;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Environments;

/// <summary>Tests for model backed material intelligence service.</summary>
public sealed class ModelBackedMaterialIntelligenceServiceTests
{
    [Fact]
    public async Task Parses_json_array_from_model()
    {
        var json = """
            [
              {"id":"m1","name":"Test","shaderName":"Universal Render Pipeline/Lit","colorHex":"#FF0000","metallic":0,"smoothness":0.5,"renderMode":"Opaque","textureSlotHints":{"_MainTex":"red brick"}}
            ]
            """;
        var mock = new Mock<IModel>();
        mock.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput(json));

        var svc = new ModelBackedMaterialIntelligenceService(mock.Object);
        var result = await svc.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "european_alley",
            new Dictionary<string, string> { ["building"] = "yes" },
            new MapDataRequestContext()));

        result.Materials.Should().HaveCount(1);
        result.Materials[0].Id.Should().Be("m1");
        result.Materials[0].TextureSlotHints["_MainTex"].Should().Be("red brick");
        result.Summary.Should().Be("model_json");
    }

    [Fact]
    public async Task Falls_back_to_heuristic_when_model_invalid()
    {
        var mock = new Mock<IModel>();
        mock.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("not json"));

        var svc = new ModelBackedMaterialIntelligenceService(mock.Object);
        var result = await svc.SuggestMaterialsAsync(new MaterialIntelligenceRequest(
            "default",
            new Dictionary<string, string> { ["building"] = "commercial" },
            new MapDataRequestContext()));

        result.Materials.Should().NotBeEmpty();
        result.Summary.Should().Contain("fallback_heuristic");
    }
}
