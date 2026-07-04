using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for aesthetic pack.</summary>
public class AestheticPackTests
{
    [Fact]
    public void DefaultAestheticPack_HasDefaults()
    {
        var pack = new AestheticPack();

        pack.Id.Should().BeEmpty();
        pack.Name.Should().BeEmpty();
        pack.GeometryStrategy.Should().Be("low_poly");
        pack.MapRenderingProfile.Should().Be(MapRenderingProfiles.Auto);
        pack.RenderingPipelineKind.Should().Be(RenderingPipelineKinds.Auto);
        pack.EngineSurfaceBindings.Should().BeEmpty();
        pack.DefaultPaletteColors.Should().BeEmpty();
        pack.LodLevels.Should().BeEmpty();
        pack.PostProcessEffects.Should().BeEmpty();
    }

    [Fact]
    public void AestheticPack_WithLodLevels_Serializes()
    {
        var pack = new AestheticPack
        {
            Id = "retro-pixel",
            Name = "Retro Pixel",
            GeometryStrategy = "pixel_art",
            DefaultPaletteColors = ["#00FF00", "#FF0000"],
            LodLevels =
            [
                new LodLevel(0, 1.0),
                new LodLevel(1, 0.5),
            ],
            PostProcessEffects = ["vignette"],
            EnvironmentManifestId = "terrain/osm-demo",
            RenderingPipelineKind = RenderingPipelineKinds.ForwardStylized,
            EngineSurfaceBindings =
            [
                new EngineRenderingSurfaceBinding
                {
                    EngineId = GameEngines.Unity,
                    Role = "world_primary",
                    MaterialSurfaceId = "stylized_lit",
                    AssetOrShaderHint = "Universal Render Pipeline/Lit",
                    Parameters = new Dictionary<string, string> { ["workflow"] = "Specular" },
                },
                new EngineRenderingSurfaceBinding
                {
                    EngineId = GameEngines.Unreal,
                    Role = "world_primary",
                    MaterialSurfaceId = "stylized_lit",
                    AssetOrShaderHint = "/Game/Materials/M_StylizedWorld",
                },
            ]
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var json = JsonSerializer.Serialize(pack, options);
        var restored = JsonSerializer.Deserialize<AestheticPack>(json, options);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("retro-pixel");
        restored.GeometryStrategy.Should().Be("pixel_art");
        restored.DefaultPaletteColors.Should().HaveCount(2);
        restored.LodLevels.Should().HaveCount(2);
        restored.LodLevels[0].Level.Should().Be(0);
        restored.LodLevels[0].DetailFactor.Should().Be(1.0);
        restored.LodLevels[1].DetailFactor.Should().Be(0.5);
        restored.PostProcessEffects.Should().ContainSingle().Which.Should().Be("vignette");
        restored.RenderingPipelineKind.Should().Be(RenderingPipelineKinds.ForwardStylized);
        restored.EngineSurfaceBindings.Should().HaveCount(2);
        restored.EngineSurfaceBindings[0].EngineId.Should().Be(GameEngines.Unity);
        restored.EngineSurfaceBindings[0].Parameters.Should().ContainKey("workflow");
        restored.EnvironmentManifestId.Should().Be("terrain/osm-demo");
    }

    [Fact]
    public void EngineSurfaceBindingResolver_ReturnsMatchingBinding()
    {
        var pack = new AestheticPack
        {
            EngineSurfaceBindings =
            [
                new EngineRenderingSurfaceBinding
                {
                    EngineId = GameEngines.Godot,
                    Role = "ui_default",
                    MaterialSurfaceId = "unlit",
                    AssetOrShaderHint = "res://ui.tres",
                },
            ]
        };

        var b = EngineSurfaceBindingResolver.TryGetBinding(pack, "GODOT", "ui_default");
        b.Should().NotBeNull();
        b!.MaterialSurfaceId.Should().Be("unlit");
    }

    [Fact]
    public void AllGeometryStrategies_AreDefined()
    {
        var expectedStrategies = new[] { "voxel", "low_poly", "pixel_art", "pbr", "wireframe", "sketch" };

        foreach (var strategy in expectedStrategies)
        {
            var pack = new AestheticPack { GeometryStrategy = strategy };
            pack.GeometryStrategy.Should().Be(strategy);
        }

        expectedStrategies.Should().HaveCount(6, "all six documented geometry strategies should be covered");
    }
}
