using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Tests.Infrastructure.Certification.Dogfood;

/// <summary>
/// Human-authored end-to-end witness for damage-to-health composition dogfood.
/// Generation and Cursor must remain blind to this.
/// </summary>
public static class CompositionDogfoodWitness
{
    public static CompositionWitnessSpec Spec => new(
        CompositionDogfoodFixtures.CompositionId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 50,
                    ["critMultiplierPercent"] = 100,
                    ["armor"] = 10,
                    ["isCrit"] = false,
                    ["currentHealth"] = 100
                },
                new Dictionary<string, object> { ["newHealth"] = 60 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 100,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 20,
                    ["isCrit"] = true,
                    ["currentHealth"] = 200
                },
                new Dictionary<string, object> { ["newHealth"] = 70 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 100,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 30,
                    ["isCrit"] = true,
                    ["currentHealth"] = 50
                },
                new Dictionary<string, object> { ["newHealth"] = 0 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 10,
                    ["critMultiplierPercent"] = 100,
                    ["armor"] = 50,
                    ["isCrit"] = false,
                    ["currentHealth"] = 25
                },
                new Dictionary<string, object> { ["newHealth"] = 25 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 7,
                    ["critMultiplierPercent"] = 150,
                    ["armor"] = 0,
                    ["isCrit"] = true,
                    ["currentHealth"] = 15
                },
                new Dictionary<string, object> { ["newHealth"] = 5 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["baseDamage"] = 40,
                    ["critMultiplierPercent"] = 100,
                    ["armor"] = 10,
                    ["isCrit"] = false,
                    ["currentHealth"] = 20
                },
                new Dictionary<string, object> { ["newHealth"] = 0 })
        ]);
}
