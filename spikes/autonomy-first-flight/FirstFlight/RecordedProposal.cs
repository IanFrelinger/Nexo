using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Spikes.FirstFlight;

/// <summary>
/// A RECORDED model proposal — the first candidate in this repository authored by a model
/// rather than a human, replayed deterministically the way the agent-composer arc replays
/// its recorded proposals (no live API in any flight).
///
/// <para><b>Provenance.</b> Authored 2026-08-14 by Claude (Fable 5) acting as the
/// proposer, from the objective text and the brick's interface contract ONLY: inputs
/// (<c>logText</c>), outputs (<c>errorCount</c>, <c>firstErrorMessage</c>), and the
/// contract "count lines containing ERROR; first message is the text after the marker,
/// trimmed of leading colon/space". The proposer did not see the witness values — the
/// generation-blind discipline the dogfood arcs established. The model identity rides
/// the lineage's proposer signature (R4.1), so it is hash-bound into the certificate's
/// <c>generation-depth</c> input, not just documented here.</para>
///
/// <para><b>Deliberately a different implementation shape</b> from the hand-authored
/// flight brick (index scanning instead of Split), so an accidental byte-copy cannot
/// masquerade as a proposal.</para>
/// </summary>
public static class RecordedProposal
{
    /// <summary>Proposer signature for the lineage (R4.1) — the model identity, hash-bound.</summary>
    public const string ProposerSignature = "model:claude-fable-5:recorded:2026-08-14";

    /// <summary>Fully-qualified type name of the proposed brick.</summary>
    public const string TypeName = "Ashlar.Spikes.FirstFlight.ProposedLogScannerBrick";

    /// <summary>The proposal, verbatim as recorded.</summary>
    public const string Source = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Spikes.FirstFlight;

public sealed class ProposedLogScannerBrick : DomainBrick
{
    public ProposedLogScannerBrick()
    {
        Id = "first-flight-log-scanner";
        Name = "Proposed Log Scanner";
        Description = "Model-proposed deterministic error-count brick.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var text = input.Get<string>("logText") ?? string.Empty;
        var errorCount = 0;
        var firstErrorMessage = string.Empty;

        var position = 0;
        while (position < text.Length)
        {
            var newline = text.IndexOfAny(new[] { '\r', '\n' }, position);
            var end = newline < 0 ? text.Length : newline;
            if (end > position)
            {
                var marker = text.IndexOf("ERROR", position, end - position, StringComparison.Ordinal);
                if (marker >= 0)
                {
                    errorCount++;
                    if (errorCount == 1)
                        firstErrorMessage = text[(marker + 5)..end].TrimStart(':', ' ');
                }
            }

            position = end + 1;
        }

        var output = new BrickOutput
        {
            Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}"
        };
        output.Set("errorCount", errorCount);
        output.Set("firstErrorMessage", firstErrorMessage);
        return Task.FromResult(output);
    }
}
""";
}

/// <summary>
/// Metadata carrier for the proposed candidate: id and declared interface only. Its
/// <c>ExecuteAsync</c> THROWS by design — with the in-session execution leg on, no
/// model-proposed code may ever run in the harness process, and if anything tries, the
/// flight must fail loudly rather than quietly executing untrusted code on the host.
/// The code that actually serves post-swap is compiled from <see cref="RecordedProposal.Source"/>
/// by the swap host, inside its generation load context.
/// </summary>
public sealed class ProposedBrickHandle : DomainBrick
{
    /// <summary>Creates the handle with the proposal's declared contract.</summary>
    public ProposedBrickHandle()
    {
        Id = "first-flight-log-scanner";
        Name = "Proposed Log Scanner";
        Description = "Model-proposed deterministic error-count brick.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
            ]
        };
    }

    /// <inheritdoc />
    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "The proposed candidate executes only inside the attested session; "
            + "in-process execution of model-proposed code is a flight failure by design.");
}
