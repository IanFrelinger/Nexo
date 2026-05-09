using System.Text.Json;
using FluentAssertions;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Scoping;

namespace Nexo.Tests.GameDomain;

public class ForgeContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ForgeGenerateRequest_Serialization()
    {
        var request = new ForgeGenerateRequest("Create a laser rifle", "weapon");

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var restored = JsonSerializer.Deserialize<ForgeGenerateRequest>(json, JsonOptions);

        restored.Should().NotBeNull();
        restored!.Prompt.Should().Be("Create a laser rifle");
        restored.Category.Should().Be("weapon");
    }

    [Fact]
    public void ForgeSettingRequest_Serialization()
    {
        var request = new ForgeSettingRequest(
            "gravity",
            9.8,
            new SettingScope { Type = SettingScopeType.Server });

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var restored = JsonSerializer.Deserialize<ForgeSettingRequest>(json, JsonOptions);

        restored.Should().NotBeNull();
        restored!.SettingId.Should().Be("gravity");
        restored.Scope.Should().NotBeNull();
        restored.Scope.Type.Should().Be(SettingScopeType.Server);
    }

    [Fact]
    public void ForgeSessionCreateRequest_Serialization()
    {
        var rules = new GameRuleDescriptor
        {
            Id = "tdm",
            Name = "Team Deathmatch",
            Mode = "team_deathmatch",
            MaxScore = 50,
            TeamCount = 2
        };
        var request = new ForgeSessionCreateRequest(rules);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var restored = JsonSerializer.Deserialize<ForgeSessionCreateRequest>(json, JsonOptions);

        restored.Should().NotBeNull();
        restored!.InitialGameMode.Should().NotBeNull();
        restored.InitialGameMode!.Id.Should().Be("tdm");
        restored.InitialGameMode.Mode.Should().Be("team_deathmatch");
    }

    [Fact]
    public void ForgeAestheticApplyRequest_Serialization()
    {
        var request = new ForgeAestheticApplyRequest(
            "neon-glow",
            new SettingScope { Type = SettingScopeType.Zone, Target = "arena-1" });

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var restored = JsonSerializer.Deserialize<ForgeAestheticApplyRequest>(json, JsonOptions);

        restored.Should().NotBeNull();
        restored!.AestheticId.Should().Be("neon-glow");
        restored.Scope.Type.Should().Be(SettingScopeType.Zone);
        restored.Scope.Target.Should().Be("arena-1");
    }

    [Fact]
    public void AllContractTypes_HaveDefaults()
    {
        var generate = new ForgeGenerateRequest("prompt", null);
        generate.Prompt.Should().Be("prompt");
        generate.Category.Should().BeNull();

        var sessionCreate = new ForgeSessionCreateRequest(null);
        sessionCreate.InitialGameMode.Should().BeNull();

        var macroRun = new ForgeMacroRunRequest("macro-1", null);
        macroRun.MacroId.Should().Be("macro-1");
        macroRun.ParameterOverrides.Should().BeNull();

        var aesthetic = new ForgeAestheticApplyRequest("pack-1", new SettingScope());
        aesthetic.AestheticId.Should().Be("pack-1");
        aesthetic.Scope.Type.Should().Be(SettingScopeType.Server);
        aesthetic.Scope.Target.Should().BeNull();
    }

    [Fact]
    public void ForgeApplyCustomAestheticPackRequest_Serialization()
    {
        var pack = new AestheticPack
        {
            Id = "custom",
            Name = "Custom",
            GeometryStrategy = GeometryStrategies.LowPoly,
            RenderingPipelineKind = RenderingPipelineKinds.ForwardStylized,
        };
        var request = new ForgeApplyCustomAestheticPackRequest(pack, new SettingScope(), RequireKnownEngineIds: true);

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var restored = JsonSerializer.Deserialize<ForgeApplyCustomAestheticPackRequest>(json, JsonOptions);

        restored.Should().NotBeNull();
        restored!.Pack.Id.Should().Be("custom");
        restored.RequireKnownEngineIds.Should().BeTrue();
    }
}
