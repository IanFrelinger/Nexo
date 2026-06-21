using Nexo.Core.Application.Certification.Models;

namespace Nexo.Tests.Infrastructure.Certification.Dogfood;

/// <summary>
/// Human-authored witness for damage-resolver dogfood. Generation must remain blind to this.
/// </summary>
public static class DamageResolverDogfoodWitness
{
    public static WitnessSpec Spec => new(
        "damage-resolver",
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 50,
                    ["critMultiplierPercent"] = 100,
                    ["armor"] = 10,
                    ["isCrit"] = false
                },
                new Dictionary<string, object> { ["finalDamage"] = 40 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 100,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 20,
                    ["isCrit"] = true
                },
                new Dictionary<string, object> { ["finalDamage"] = 130 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 100,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 30,
                    ["isCrit"] = true
                },
                new Dictionary<string, object> { ["finalDamage"] = 120 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 10,
                    ["critMultiplierPercent"] = 100,
                    ["armor"] = 50,
                    ["isCrit"] = false
                },
                new Dictionary<string, object> { ["finalDamage"] = 0 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 7,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 0,
                    ["isCrit"] = true
                },
                new Dictionary<string, object> { ["finalDamage"] = 10 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 0,
                    ["critMultiplierPercent"] = 200,
                    ["armor"] = 5,
                    ["isCrit"] = true
                },
                new Dictionary<string, object> { ["finalDamage"] = 0 })
        ]);
}
