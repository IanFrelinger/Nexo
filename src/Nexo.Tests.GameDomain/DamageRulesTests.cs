using FluentAssertions;
using Nexo.GameDomain.Contracts;
using Nexo.GameDomain.Rules.Combat;

namespace Nexo.Tests.GameDomain;

public sealed class DamageRulesTests
{
    [Theory]
    [InlineData(100, 150, true, 20, 200, 150, 130, 70)]
    [InlineData(100, 150, false, 20, 200, 100, 80, 120)]
    [InlineData(10, 200, true, 50, 100, 20, 0, 100)]
    [InlineData(100, 100, false, 0, 25, 100, 25, 0)]
    public void Resolve_UsesTypedIntegerRules(
        int baseDamage,
        int criticalMultiplier,
        bool critical,
        int armor,
        int health,
        int expectedRaw,
        int expectedApplied,
        int expectedHealth)
    {
        var command = new DamageCommand(
            new EntityId(1),
            new EntityId(2),
            baseDamage,
            criticalMultiplier,
            critical);
        var context = new DamageContext(health, armor);

        var result = DamageRules.Resolve(command, context);

        result.RawDamage.Should().Be(expectedRaw);
        result.AppliedDamage.Should().Be(expectedApplied);
        result.RemainingHealth.Should().Be(expectedHealth);
        result.Eliminated.Should().Be(expectedHealth == 0);
    }

    [Fact]
    public void Resolve_ClampsInvalidAndOverflowingInputs()
    {
        var command = new DamageCommand(
            new EntityId(1),
            new EntityId(2),
            int.MaxValue,
            int.MaxValue,
            isCritical: true);

        var result = DamageRules.Resolve(command, new DamageContext(100, -10));

        result.RawDamage.Should().Be(int.MaxValue);
        result.AppliedDamage.Should().Be(100);
        result.RemainingHealth.Should().Be(0);
    }
}
