using FluentAssertions;
using Nexo.GameDomain.Aesthetics;
using Xunit;

namespace Nexo.Tests.GameDomain;

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
                new EngineSurfaceBinding
                {
                    EngineId = "unity",
                    Role = "world_primary",
                    MaterialSurfaceId = "lit",
                    AssetOrShaderHint = "HDRP/Lit"
                },
                new EngineSurfaceBinding
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
