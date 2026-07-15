using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain.Bricks;

/// <summary>
/// Computes final damage from base damage, crit multiplier, armor, and crit flag.
/// Same contract as the certified damage-resolver sample atom.
/// </summary>
public sealed class DamageResolverBrick : DeterministicGameplayBrick
{
    /// <summary>Creates the damage resolver brick.</summary>
    public DamageResolverBrick()
    {
        Id = "damage-resolver";
        Name = "Damage Resolver";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Computes final damage from base damage, crit multiplier, armor, and crit flag.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("baseDamage", "int", "Base damage before modifiers"),
                new BrickInputDefinition("critMultiplierPercent", "int", "Crit multiplier percent (150 = 1.5x)"),
                new BrickInputDefinition("armor", "int", "Armor subtracted after crit"),
                new BrickInputDefinition("isCrit", "bool", "Whether the hit is a critical strike")
            ],
            Outputs = [new BrickOutputDefinition("finalDamage", "int", "Final damage dealt")]
        };
    }

    /// <inheritdoc />
    protected override BrickOutput ExecuteDeterministic(BrickInput input, IExecutionContext context)
    {
        var baseDamage = input.Get<int>("baseDamage");
        var critMultiplierPercent = input.Get<int>("critMultiplierPercent");
        var armor = input.Get<int>("armor");
        var isCrit = input.Get<bool>("isCrit");

        var raw = isCrit
            ? baseDamage * critMultiplierPercent / 100
            : baseDamage;
        var finalDamage = Math.Max(0, raw - armor);

        var output = new BrickOutput { Summary = $"Final damage: {finalDamage}" };
        output.Set("finalDamage", finalDamage);
        return output;
    }
}
