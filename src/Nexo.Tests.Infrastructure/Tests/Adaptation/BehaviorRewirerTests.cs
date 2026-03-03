using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Infrastructure.Adaptation;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Adaptation;

[Trait("Category", "Adaptation")]
public sealed class BehaviorRewirerTests
{
    private static Behavior CreateBehavior()
    {
        return new Behavior
        {
            Id = "beh1",
            Name = "Test Behavior",
            Version = "1.0.0",
            Description = "Test",
            Steps =
            [
                new BehaviorStep { Id = "s1", BrickId = "brick-a", InputMapping = new Dictionary<string, string>(), OutputMapping = new Dictionary<string, string>() },
                new BehaviorStep { Id = "s2", BrickId = "brick-b", InputMapping = new Dictionary<string, string>(), OutputMapping = new Dictionary<string, string>() },
            ],
        };
    }

    [Fact]
    public async Task RewireAsync_BrickSwap_UpdatesStepBrickId()
    {
        var rewirer = new BehaviorRewirer();
        var behavior = CreateBehavior();
        var changes = new BehaviorRewireChanges
        {
            BrickSwaps = new Dictionary<string, string> { ["s1"] = "brick-x" },
        };

        var result = await rewirer.RewireAsync(behavior, changes);

        result.Steps[0].BrickId.Should().Be("brick-x");
        result.Steps[1].BrickId.Should().Be("brick-b");
    }

    [Fact]
    public async Task RewireAsync_StepOrder_ReordersSteps()
    {
        var rewirer = new BehaviorRewirer();
        var behavior = CreateBehavior();
        var changes = new BehaviorRewireChanges
        {
            StepOrder = ["s2", "s1"],
        };

        var result = await rewirer.RewireAsync(behavior, changes);

        result.Steps[0].Id.Should().Be("s2");
        result.Steps[1].Id.Should().Be("s1");
    }

    [Fact]
    public async Task RewireAsync_InputMappingOverride_UpdatesMapping()
    {
        var rewirer = new BehaviorRewirer();
        var behavior = CreateBehavior();
        var changes = new BehaviorRewireChanges
        {
            InputMappingOverrides = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["s1"] = new Dictionary<string, string> { ["input1"] = "value1" },
            },
        };

        var result = await rewirer.RewireAsync(behavior, changes);

        result.Steps[0].InputMapping.Should().ContainKey("input1");
        result.Steps[0].InputMapping["input1"].Should().Be("value1");
    }

    [Fact]
    public async Task RewireAsync_NoChanges_ReturnsEquivalentBehavior()
    {
        var rewirer = new BehaviorRewirer();
        var behavior = CreateBehavior();
        var changes = new BehaviorRewireChanges();

        var result = await rewirer.RewireAsync(behavior, changes);

        result.Steps.Should().HaveCount(2);
        result.Steps[0].BrickId.Should().Be("brick-a");
        result.Steps[1].BrickId.Should().Be("brick-b");
    }
}
