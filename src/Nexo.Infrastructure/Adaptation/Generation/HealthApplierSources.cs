using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>Generated source templates for health applier certification bricks.</summary>
internal static class HealthApplierSources
{
    /// <summary>Returns correct health applier brick source for the given witness signature.</summary>
    public static string Honest(WitnessSignature signature) => $$"""
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Certified.DamageResolver;

public sealed class HealthApplierBrick : DomainBrick
{
    public HealthApplierBrick()
    {
        Id = "{{signature.BrickId}}";
        Name = "Health Applier";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Applies final damage to current health, flooring at zero.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("currentHealth", "int", "Health before damage"),
                new BrickInputDefinition("finalDamage", "int", "Damage to subtract")
            ],
            Outputs = [new BrickOutputDefinition("newHealth", "int", "Health after damage")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var currentHealth = input.Get<int>("currentHealth");
        var finalDamage = input.Get<int>("finalDamage");
        var newHealth = Math.Max(0, currentHealth - finalDamage);

        var output = new BrickOutput { Summary = $"New health: {newHealth}" };
        output.Set("newHealth", newHealth);
        return Task.FromResult(output);
    }
}
""";
}
