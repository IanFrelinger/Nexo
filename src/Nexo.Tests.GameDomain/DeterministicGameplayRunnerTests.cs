using FluentAssertions;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain;

namespace Nexo.Tests.GameDomain;

public sealed class DeterministicGameplayRunnerTests
{
    [Fact]
    public async Task Runner_ExecutesRegisteredBrick()
    {
        var registry = GameplayBrickRegistry.CreateDefault();
        var runner = new DeterministicGameplayRunner(registry);

        var input = new BrickInput();
        input.Set("baseDamage", 100);
        input.Set("critMultiplierPercent", 150);
        input.Set("armor", 20);
        input.Set("isCrit", true);

        var output = await runner.ExecuteAsync("damage-resolver", input);

        output.Get<int>("finalDamage").Should().Be(130);
        registry.GetAllBricks().Should().HaveCount(3);
    }

    [Fact]
    public async Task Runner_Throws_WhenBrickMissing()
    {
        var runner = new DeterministicGameplayRunner(GameplayBrickRegistry.CreateDefault());
        var act = () => runner.ExecuteAsync("missing-brick", new BrickInput());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
