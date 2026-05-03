using FluentAssertions;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Mapping;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;

namespace Nexo.Tests.GameDomain;

public class MapAdaptationPlannerTests
{
    [Theory]
    [InlineData("voxel", MapRenderingProfiles.VoxelGrid)]
    [InlineData("low_poly", MapRenderingProfiles.FlatShadedPolys)]
    [InlineData("pixel_art", MapRenderingProfiles.OrthographicTile)]
    [InlineData("pbr", MapRenderingProfiles.HeightfieldMesh)]
    [InlineData("wireframe", MapRenderingProfiles.VectorOverlay)]
    [InlineData("sketch", MapRenderingProfiles.VectorOverlay)]
    public void ResolveEffectiveProfile_Auto_InfersFromGeometry(string strategy, string expected)
    {
        var pack = new AestheticPack { GeometryStrategy = strategy, MapRenderingProfile = MapRenderingProfiles.Auto };
        MapAdaptationPlanner.ResolveEffectiveProfile(pack).Should().Be(expected);
    }

    [Fact]
    public void ResolveEffectiveProfile_Explicit_ReturnsAsIs()
    {
        var pack = new AestheticPack
        {
            GeometryStrategy = "voxel",
            MapRenderingProfile = MapRenderingProfiles.VectorOverlay
        };
        MapAdaptationPlanner.ResolveEffectiveProfile(pack).Should().Be(MapRenderingProfiles.VectorOverlay);
    }

    [Fact]
    public void BuildPlan_WithAppliedAesthetic_UsesPackProfile()
    {
        var session = new SessionState();
        session.ScopedSettings.Add(new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = "voxel",
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "t",
            CreatedAt = DateTimeOffset.UtcNow
        });
        session.AestheticPacks.Add(new AestheticPack
        {
            Id = "voxel",
            Name = "Voxel",
            GeometryStrategy = "voxel",
            MapRenderingProfile = MapRenderingProfiles.VoxelGrid
        });

        var plan = MapAdaptationPlanner.BuildPlan(session);
        plan.ActiveAestheticId.Should().Be("voxel");
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.VoxelGrid);
        plan.ProfileWasInferred.Should().BeFalse();
        plan.PipelineStages.Should().Contain("voxel_rasterize");
    }

    [Fact]
    public void BuildPlan_MissingPackOnSession_UsesCatalogFallback()
    {
        var builtIn = new[]
        {
            new AestheticPack
            {
                Id = "pbr",
                Name = "PBR",
                GeometryStrategy = "pbr",
                MapRenderingProfile = MapRenderingProfiles.HeightfieldMesh
            }
        };

        var session = new SessionState();
        session.ScopedSettings.Add(new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = "pbr",
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "t",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var plan = MapAdaptationPlanner.BuildPlan(session, builtIn);
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.HeightfieldMesh);
        plan.PipelineStages.Should().Contain("mesh_emit");
    }
}
