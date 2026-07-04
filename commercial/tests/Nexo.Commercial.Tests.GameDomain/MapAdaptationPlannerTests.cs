using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Mapping;
using Nexo.Commercial.GameDomain.Scoping;
using Nexo.Commercial.GameDomain.Session;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for map adaptation planner.</summary>
public sealed class MapAdaptationPlannerTests
{
    private static readonly IReadOnlyList<AestheticPack> Catalog =
    [
        new AestheticPack { Id = "voxel", Name = "V", GeometryStrategy = "voxel", MapRenderingProfile = MapRenderingProfiles.VoxelGrid },
        new AestheticPack { Id = "pbr", Name = "P", GeometryStrategy = "pbr", MapRenderingProfile = MapRenderingProfiles.Auto }
    ];

    [Fact]
    public void Plan_UsesServerAestheticSetting()
    {
        var session = new SessionState();
        session.ScopedSettings.Add(new ScopedSetting
        {
            SettingId = "aesthetic",
            Value = "pbr",
            Scope = new SettingScope { Type = SettingScopeType.Server },
            CreatedBy = "t",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var plan = MapAdaptationPlanner.Plan(session, Catalog);
        plan.GeometryStrategy.Should().Be("pbr");
        plan.EffectiveMapRenderingProfile.Should().Be(MapRenderingProfiles.HeightfieldMesh);
        plan.PipelineStages.Should().Contain("fetch_terrain");
    }

    [Fact]
    public void DryRun_AllStagesSimulated()
    {
        var session = new SessionState();
        var plan = MapAdaptationPlanner.Plan(session, Catalog);
        var result = MapPipelineDryRun.Execute(plan, new MapPipelineRunRequest(DryRun: true));
        result.Success.Should().BeTrue();
        result.Stages.Should().HaveCount(plan.PipelineStages.Count);
    }
}
