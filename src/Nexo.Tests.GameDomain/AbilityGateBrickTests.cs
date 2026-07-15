using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain;
using Nexo.GameDomain.Bricks;

namespace Nexo.Tests.GameDomain;

public sealed class AbilityGateBrickTests
{
    [Fact]
    public async Task Allows_WhenReadyAndAffordable()
    {
        var input = new BrickInput();
        input.Set("cooldownRemainingMs", 0);
        input.Set("resourceAvailable", 50);
        input.Set("resourceCost", 20);
        input.Set("abilityId", "dash");

        var output = await new AbilityGateBrick().ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            GameplayExecutionContext.ForPlayer("p1"));

        output.Get<bool>("allowed").Should().BeTrue();
        output.Get<int>("resourceAfter").Should().Be(30);
        output.Get<string>("denyReason").Should().BeEmpty();
    }

    [Fact]
    public async Task Denies_OnCooldown()
    {
        var input = new BrickInput();
        input.Set("cooldownRemainingMs", 250);
        input.Set("resourceAvailable", 50);
        input.Set("resourceCost", 20);

        var output = await new AbilityGateBrick().ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            GameplayExecutionContext.ForPlayer("p1"));

        output.Get<bool>("allowed").Should().BeFalse();
        output.Get<string>("denyReason").Should().Be("cooldown");
        output.Get<int>("resourceAfter").Should().Be(50);
    }
}
