using Nexo.Certification.Contracts;
using Nexo.Certification.State;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Tier-2 replayer that re-applies certified brick source to reproduce transition state hashes.
/// </summary>
public sealed class BrickTransitionReplayer : ITransitionReplayer
{
    private readonly StateSchema _schema;
    private readonly CertifiedBrickInstantiator _instantiator;
    private readonly StateTransitionReplayExecutionContext _context = new();

    public BrickTransitionReplayer(StateSchema schema, CertifiedBrickInstantiator? instantiator = null)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _instantiator = instantiator ?? new CertifiedBrickInstantiator();
    }

    public TransitionReplayResult Replay(
        string priorStateHash,
        string action,
        CertificationRecordData behaviorCert,
        string brickSource)
    {
        if (string.IsNullOrWhiteSpace(priorStateHash))
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-prior-state-invalid",
                "Prior state hash is required.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-action-invalid",
                "Transition action is required.");
        }

        if (behaviorCert is null)
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-behavior-invalid",
                "Behavior certification record is required.");
        }

        if (string.IsNullOrWhiteSpace(brickSource))
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-source-invalid",
                "Certified brick source is required.");
        }

        var expectedContentHash = BrickContentHasher.ComputeSha256(brickSource);
        if (!string.Equals(behaviorCert.ContentHash, expectedContentHash, StringComparison.Ordinal))
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-content-hash-mismatch",
                "Certified brick source does not match the behavior certification content hash.");
        }

        try
        {
            var brick = _instantiator.Instantiate(brickSource);
            var input = new BrickInput(new Dictionary<string, object>
            {
                ["priorStateHash"] = priorStateHash,
                ["action"] = action,
                ["schemaHash"] = _schema.SchemaHash,
                ["bindingVersion"] = _schema.StateBindingVersion
            });

            var output = brick.ExecuteAsync(
                input,
                ImplementationType.Deterministic,
                _context,
                CancellationToken.None).GetAwaiter().GetResult();

            if (!output.ToDictionary().TryGetValue("resultingStateHash", out var raw)
                || raw is not string computed
                || string.IsNullOrWhiteSpace(computed))
            {
                return new TransitionReplayResult(
                    false,
                    null,
                    "replay-output-missing",
                    "Certified behavior did not emit resultingStateHash.");
            }

            if (!_schema.IsValidStateHash(computed))
            {
                return new TransitionReplayResult(
                    false,
                    computed,
                    "replay-output-invalid",
                    "Certified behavior emitted a state hash that violates the schema.");
            }

            return new TransitionReplayResult(true, computed, null, null);
        }
        catch (Exception ex)
        {
            return new TransitionReplayResult(
                false,
                null,
                "replay-execution-failed",
                ex.Message);
        }
    }
}
