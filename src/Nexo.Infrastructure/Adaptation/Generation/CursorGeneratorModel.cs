using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// <strong>TEST DOUBLE</strong> — stand-in for <see cref="ProviderGeneratorModel"/> in dogfood certification tests.
/// Returns Cursor-authored source written at sprint time (not a live API or production generation).
/// Witness cases are never seen — only <see cref="WitnessSignature"/> I/O.
/// </summary>
public sealed class CursorGeneratorModel : IGeneratorModel
{
    public const string DamageResolverIntentId = "damage-resolver";

    /// <summary>honest | buggy</summary>
    public string Variant { get; set; } = "honest";

    public Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(intent.IntentId, DamageResolverIntentId, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Cursor model has no source for intent '{intent.IntentId}'.");

        var source = Variant switch
        {
            "buggy" => DamageResolverSources.Buggy(witnessSignature),
            _ => DamageResolverSources.Honest(witnessSignature)
        };

        return Task.FromResult(new GeneratedBrickSource(
            source,
            Provenance: $"cursor:{Variant}",
            ClassName: "DamageResolverBrick",
            Namespace: "GeneratedBricks"));
    }
}

internal static class DamageResolverSources
{
    public static string Honest(WitnessSignature signature) => $$"""
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GeneratedBricks;

public sealed class DamageResolverBrick : Brick
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
