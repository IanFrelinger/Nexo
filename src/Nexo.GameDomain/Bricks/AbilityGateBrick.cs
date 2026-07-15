using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain.Bricks;

/// <summary>
/// Gates an ability on remaining cooldown and resource cost.
/// </summary>
public sealed class AbilityGateBrick : DeterministicGameplayBrick
{
    /// <summary>Creates the ability gate brick.</summary>
    public AbilityGateBrick()
    {
        Id = "ability-gate";
        Name = "Ability Gate";
        Version = "1.0.0";
        Category = BrickCategory.Validation;
        Description = "Allows an ability when cooldown has elapsed and resources cover the cost.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("cooldownRemainingMs", "int", "Milliseconds remaining on cooldown"),
                new BrickInputDefinition("resourceAvailable", "int", "Current resource pool"),
                new BrickInputDefinition("resourceCost", "int", "Cost to activate"),
                new BrickInputDefinition("abilityId", "string", "Ability identifier", required: false, defaultValue: "ability")
            ],
            Outputs =
            [
                new BrickOutputDefinition("allowed", "bool", "Whether the ability may activate"),
                new BrickOutputDefinition("denyReason", "string", "Reason when not allowed"),
                new BrickOutputDefinition("resourceAfter", "int", "Resource remaining if allowed (else unchanged)")
            ]
        };
    }

    /// <inheritdoc />
    protected override BrickOutput ExecuteDeterministic(BrickInput input, IExecutionContext context)
    {
        var cooldownRemainingMs = input.Get<int>("cooldownRemainingMs");
        var resourceAvailable = input.Get<int>("resourceAvailable");
        var resourceCost = input.Get<int>("resourceCost");
        var abilityId = input.Get("abilityId", "ability") ?? "ability";

        string? denyReason = null;
        if (cooldownRemainingMs > 0)
            denyReason = "cooldown";
        else if (resourceCost < 0)
            denyReason = "invalid-cost";
        else if (resourceAvailable < resourceCost)
            denyReason = "insufficient-resource";

        var allowed = denyReason is null;
        var resourceAfter = allowed ? resourceAvailable - resourceCost : resourceAvailable;

        var output = new BrickOutput
        {
            Summary = allowed
                ? $"Ability '{abilityId}' allowed."
                : $"Ability '{abilityId}' denied: {denyReason}."
        };
        output.Set("allowed", allowed);
        output.Set("denyReason", denyReason ?? string.Empty);
        output.Set("resourceAfter", resourceAfter);
        return output;
    }
}
