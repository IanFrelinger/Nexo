using FluentAssertions;
using Ashlar.CLI.Commands;
using Xunit;

namespace Ashlar.Tests.CLI;

/// <summary>Tests for unity dev command.</summary>
public class UnityDevCommandTests
{
    private static readonly string[] NewAssetTypes = ["vfx", "physics", "input", "network", "ainavigation", "build"];

    [Theory]
    [InlineData("vfx")]
    [InlineData("physics")]
    [InlineData("input")]
    [InlineData("network")]
    [InlineData("ainavigation")]
    [InlineData("build")]
    public void BuildAssetPrompt_AcceptsNewType(string assetType)
    {
        var prompt = UnityDevCommand.BuildAssetPrompt(assetType, $"Test {assetType} description");

        prompt.Should().NotBeNullOrWhiteSpace();
        prompt.Should().Contain($"Asset type: {assetType}");
        prompt.Should().Contain($"Test {assetType} description");
    }

    [Theory]
    [InlineData("vfx", "VfxDescriptor")]
    [InlineData("physics", "PhysicsDescriptor")]
    [InlineData("input", "InputDescriptor")]
    [InlineData("network", "NetworkDescriptor")]
    [InlineData("ainavigation", "AiNavigationDescriptor")]
    [InlineData("build", "BuildDescriptor")]
    public void BuildAssetPrompt_ContainsSchemaHint(string assetType, string expectedHint)
    {
        var prompt = UnityDevCommand.BuildAssetPrompt(assetType, "test");

        prompt.Should().Contain(expectedHint);
    }

    [Fact]
    public void BuildAssetPrompt_AllOriginalTypesStillWork()
    {
        var originalTypes = new[] { "material", "prefab", "scene", "audio", "animation", "soundbank", "animationset", "ui" };

        foreach (var type in originalTypes)
        {
            var prompt = UnityDevCommand.BuildAssetPrompt(type, "test");
            prompt.Should().NotBeNullOrWhiteSpace($"type '{type}' should produce a prompt");
            prompt.Should().Contain($"Asset type: {type}");
        }
    }
}
