using FluentAssertions;
using Ashlar.Commercial.GameDomain.Aesthetics;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for engine aesthetic manifest builder.</summary>
public sealed class EngineAestheticManifestBuilderTests
{
    [Fact]
    public void BuildJson_FiltersBindingsByEngine()
    {
        var pack = new AestheticPack
        {
            Id = "demo",
            Name = "Demo",
            GeometryStrategy = "low_poly",
            MapRenderingProfile = MapRenderingProfiles.FlatShadedPolys,
            EngineSurfaceBindings =
            [
                new EngineRenderingSurfaceBinding
                {
                    EngineId = "unity",
                    Role = "world_primary",
                    MaterialSurfaceId = "lit",
                    AssetOrShaderHint = "HDRP/Lit",
                },
                new EngineRenderingSurfaceBinding
                {
                    EngineId = "godot",
                    Role = "world_primary",
                    MaterialSurfaceId = "standard",
                }
            ]
        };

        var json = EngineAestheticManifestBuilder.BuildJson("unity", pack);
        json.Should().Contain("\"engineId\"");
        json.Should().Contain("unity");
        json.Should().Contain("HDRP/Lit");
        json.Should().NotContain("godot");
    }
}
