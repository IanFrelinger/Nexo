using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;

namespace Ashlar.Infrastructure.Adaptation.Generation;

/// <summary>Generated source templates for damage resolver certification bricks.</summary>
internal static class DamageResolverSources
{
    /// <summary>Returns correct damage resolver brick source for the given witness signature.</summary>
    public static string Honest(WitnessSignature signature) => $$"""
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Certified.DamageResolver;

public sealed class DamageResolverBrick : DomainBrick
{
    public DamageResolverBrick()
    {
        Id = "{{signature.BrickId}}";
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

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
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
        return Task.FromResult(output);
    }
}
""";

    /// <summary>Returns a buggy variant that applies armor before the crit multiplier.</summary>
    public static string Buggy(WitnessSignature signature)
    {
        var honest = Honest(signature);
        return honest
            .Replace(
                """
        var raw = isCrit
            ? baseDamage * critMultiplierPercent / 100
            : baseDamage;
        var finalDamage = Math.Max(0, raw - armor);
""",
                """
        var afterArmor = baseDamage - armor;
        var raw = isCrit
            ? afterArmor * critMultiplierPercent / 100
            : afterArmor;
        var finalDamage = raw;
""",
                StringComparison.Ordinal);
    }
}
