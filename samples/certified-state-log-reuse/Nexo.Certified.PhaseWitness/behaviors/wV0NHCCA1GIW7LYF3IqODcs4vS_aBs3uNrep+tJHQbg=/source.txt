using System.Security.Cryptography;
using System.Text;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GeneratedWitness;

public sealed class PhaseAdvanceBrick : Brick
{
    public PhaseAdvanceBrick()
    {
        Id = "behavior-alpha";
        Name = "Phase Advance";
        Version = "1.0.0";
        Category = BrickCategory.Analysis;
        Description = "Advances witness phase from genesis to idle, or idle to armed.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("priorStateHash", "string", "Schema-bound hash of prior state"),
                new BrickInputDefinition("action", "string", "Transition action label"),
                new BrickInputDefinition("schemaHash", "string", "State schema content hash"),
                new BrickInputDefinition("bindingVersion", "string", "State binding version from schema")
            ],
            Outputs = [new BrickOutputDefinition("resultingStateHash", "string", "Schema-bound hash of next state")]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var priorStateHash = input.Get<string>("priorStateHash");
        var action = input.Get<string>("action");
        var schemaHash = input.Get<string>("schemaHash");
        var bindingVersion = input.Get<string>("bindingVersion");

        if (!string.Equals(action, "phase:advance", StringComparison.Ordinal))
        {
            return Task.FromResult(Failure("Action is not supported by phase advance behavior."));
        }

        string? nextPayload = null;
        if (MatchesBound(priorStateHash, schemaHash, bindingVersion, "genesis"))
            nextPayload = "phase:idle";
        else if (MatchesBound(priorStateHash, schemaHash, bindingVersion, "phase:idle"))
            nextPayload = "phase:armed";

        if (nextPayload is null)
        {
            return Task.FromResult(Failure("Prior state and action are not admitted by phase advance behavior."));
        }

        var output = new BrickOutput { Summary = $"Advanced to {nextPayload}" };
        output.Set("resultingStateHash", ComputeBoundStateHash(schemaHash, bindingVersion, nextPayload));
        return Task.FromResult(output);
    }

    private static bool MatchesBound(string hash, string schemaHash, string bindingVersion, string payload) =>
        string.Equals(hash, ComputeBoundStateHash(schemaHash, bindingVersion, payload), StringComparison.Ordinal);

    private static string ComputeBoundStateHash(string schemaHash, string bindingVersion, string payload)
    {
        var bound = $"STATE|{bindingVersion}|{schemaHash}|{payload}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(bound));
        return Convert.ToBase64String(hash);
    }

    private static BrickOutput Failure(string summary) =>
        new() { Summary = summary };
}
