using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GameDomain;
using Nexo.GameDomain.Bricks;

namespace Nexo.Tests.GameDomain;

public sealed class DamageResolverBrickTests
{
    [Theory]
    [InlineData(100, 150, 20, true, 130)]
    [InlineData(100, 150, 20, false, 80)]
    [InlineData(10, 200, 50, true, 0)]
    public async Task Execute_ComputesFinalDamage(
        int baseDamage,
        int critPercent,
        int armor,
        bool isCrit,
        int expected)
    {
        var brick = new DamageResolverBrick();
        var input = new BrickInput();
        input.Set("baseDamage", baseDamage);
        input.Set("critMultiplierPercent", critPercent);
        input.Set("armor", armor);
        input.Set("isCrit", isCrit);

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            GameplayExecutionContext.ForPlayer("p1"));

        output.Get<int>("finalDamage").Should().Be(expected);
    }

    [Fact]
    public async Task Agentic_Throws()
    {
        var brick = new DamageResolverBrick();
        var act = () => brick.ExecuteAsync(
            new BrickInput(),
            ImplementationType.Agentic,
            GameplayExecutionContext.ForPlayer("p1"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deterministic-only*");
    }
}
