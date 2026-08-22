using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.BackgroundAgents.Autonomy;

/// <summary>
/// Identity-only stand-in for a proposed brick. The certification gate needs a brick
/// object to read the id from; with an execution backend present it never executes this
/// instance (witness, determinism and mutation all run in the session), and the swap host
/// reads the real contract off the COMPILED type, so this handle's interface is never
/// consulted.
///
/// <para><c>ExecuteAsync</c> therefore throws rather than returning anything: proposed
/// code must never run in the host process, and if some future path tries, it should fail
/// loudly here instead of quietly executing untrusted code in-proc. The gate records that
/// throw as a <see cref="WitnessFindingKind.Threw"/> finding on every case;
/// <see cref="RefusedInProcessExecution"/> recognises the resulting rejection so the loop
/// can report it as host wiring rather than hand it to a proposer as something to repair.</para>
/// </summary>
internal sealed class ProposedBrickHandle : DomainBrick
{
    /// <summary>
    /// The refusal's fixed text; the witness runner keeps only an exception's message, so
    /// this sentence is what a rejection carries and what <see cref="RefusedInProcessExecution"/>
    /// looks for.
    /// </summary>
    internal const string InProcessRefusal =
        "must execute inside the attested session; in-process execution of proposed code is a wiring bug, not a fallback";

    public ProposedBrickHandle(string brickId)
    {
        Id = brickId;
        Name = brickId;
        Description = "Proposed candidate (identity handle; executes only inside the session).";
        Interface = new BrickInterface { Inputs = [], Outputs = [] };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException($"Proposed brick '{Id}' {InProcessRefusal}.");

    /// <summary>
    /// True when a rejection is this handle refusing to run in-process: a correctness
    /// rejection whose every witness finding is a throw carrying <see cref="InProcessRefusal"/>.
    /// A candidate that genuinely threw on some cases and answered others is NOT this — that
    /// is a real defect and stays repairable.
    /// </summary>
    internal static bool RefusedInProcessExecution(CertificationDecision decision)
    {
        if (decision.Admitted || decision.WitnessFindings.Count == 0)
            return false;

        return decision.WitnessFindings.All(f =>
            f.Kind == WitnessFindingKind.Threw
            && f.Detail is { } detail
            && detail.Contains(InProcessRefusal, StringComparison.Ordinal));
    }
}
