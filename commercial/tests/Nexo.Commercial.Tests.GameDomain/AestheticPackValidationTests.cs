using FluentAssertions;
using Nexo.GameDomain.Aesthetics;
using Xunit;

namespace Nexo.Tests.GameDomain;

public class AestheticPackValidationTests
{
    [Fact]
    public void Validate_BuiltInLikePack_HasNoBlockingIssues()
    {
        var pack = new AestheticPack
        {
            Id = "low_poly",
            Name = "Low Poly",
            GeometryStrategy = GeometryStrategies.LowPoly,
            RenderingPipelineKind = RenderingPipelineKinds.ForwardStylized,
        };

        var issues = AestheticPackValidation.Validate(pack);
        AestheticPackValidation.IsValid(issues).Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownGeometryStrategy_Fails()
    {
        var pack = new AestheticPack
        {
            Id = "x",
            GeometryStrategy = "not_a_strategy",
            RenderingPipelineKind = RenderingPipelineKinds.Auto,
        };

        var issues = AestheticPackValidation.Validate(pack);
        issues.Should().Contain(i => i.Code == "aesthetic.unknown_geometry_strategy");
        AestheticPackValidation.IsValid(issues).Should().BeFalse();
    }

    [Fact]
    public void Validate_UnknownPipelineKind_Fails()
    {
        var pack = new AestheticPack
        {
            Id = "x",
            GeometryStrategy = GeometryStrategies.Pbr,
            RenderingPipelineKind = "weird_pipeline",
        };

        var issues = AestheticPackValidation.Validate(pack);
        issues.Should().Contain(i => i.Code == "aesthetic.unknown_rendering_pipeline_kind");
        AestheticPackValidation.IsValid(issues).Should().BeFalse();
    }

    [Fact]
    public void Validate_DuplicateEngineRole_Fails()
    {
        var pack = new AestheticPack
        {
            Id = "dup",
            GeometryStrategy = GeometryStrategies.LowPoly,
            RenderingPipelineKind = RenderingPipelineKinds.Auto,
            EngineSurfaceBindings =
            [
                new EngineRenderingSurfaceBinding
                {
                    EngineId = GameEngines.Unity,
                    Role = RenderingSurfaceRoles.WorldPrimary,
                    MaterialSurfaceId = MaterialSurfaceIds.StylizedLit,
                },
                new EngineRenderingSurfaceBinding
                {
                    EngineId = GameEngines.Unity,
                    Role = RenderingSurfaceRoles.WorldPrimary,
                    MaterialSurfaceId = MaterialSurfaceIds.ForwardPbr,
                },
            ]
        };

        var issues = AestheticPackValidation.Validate(pack);
        issues.Should().Contain(i => i.Code == "binding.duplicate_engine_role");
        AestheticPackValidation.IsValid(issues).Should().BeFalse();
    }

    [Fact]
    public void Validate_UnknownEngineId_WhenRequired_Fails()
    {
        var pack = new AestheticPack
        {
            Id = "e",
            GeometryStrategy = GeometryStrategies.LowPoly,
            RenderingPipelineKind = RenderingPipelineKinds.Auto,
            EngineSurfaceBindings =
            [
                new EngineRenderingSurfaceBinding
                {
                    EngineId = "bevy",
                    Role = RenderingSurfaceRoles.WorldPrimary,
                    MaterialSurfaceId = MaterialSurfaceIds.Unlit,
                },
            ]
        };

        var strict = new AestheticPackValidationOptions { RequireKnownEngineIds = true };
        var issues = AestheticPackValidation.Validate(pack, strict);
        issues.Should().Contain(i => i.Code == "binding.unknown_engine_id");
        AestheticPackValidation.IsValid(issues).Should().BeFalse();
    }
}
