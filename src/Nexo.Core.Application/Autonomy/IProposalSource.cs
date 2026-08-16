namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// What a proposer is given: the objective's declarations and nothing else. Note what is
/// absent — the witness. Proposers are generation-blind by construction (the discipline the
/// dogfood arcs established): a proposer that saw the acceptance cases could satisfy them
/// without satisfying the contract, and the certificate would then be a claim about nothing.
/// </summary>
/// <param name="ObjectiveId">Objective id; also the lineage key.</param>
/// <param name="Title">Objective title.</param>
/// <param name="Body">Objective body — the human-authored statement of intent.</param>
/// <param name="Touch">Declared touch-set (blast radius), or null when undeclared.</param>
public sealed record ProposalRequest(
    string ObjectiveId,
    string Title,
    string Body,
    TouchSet? Touch)
{
    /// <summary>
    /// Repair context when this is a re-proposal after a rejection: the proposer's own
    /// previous source and the gate's feedback, ALREADY PROJECTED by the host's
    /// <c>RepairFeedbackPolicy</c>. A proposer never sees the raw rejection; what arrives
    /// here is exactly what policy allows and nothing more. Null on a first proposal.
    /// </summary>
    public RepairContext? Repair { get; init; }
}

/// <summary>What a proposer is handed to repair with. See <see cref="ProposalRequest.Repair"/>.</summary>
/// <param name="PreviousSource">The proposer's own rejected source.</param>
/// <param name="Feedback">Policy-projected feedback: locations and the proposer's own output, never the witness (unless the operator opted into full disclosure).</param>
/// <param name="Attempt">1-based repair attempt number.</param>
public sealed record RepairContext(string PreviousSource, string Feedback, int Attempt);

/// <summary>
/// One proposed candidate. Source only: the proposer produces code, never a verdict about
/// it, and never its own acceptance criteria.
/// </summary>
/// <param name="SourceCode">Proposed brick source.</param>
/// <param name="TypeName">Fully-qualified type name of the proposed brick.</param>
/// <param name="ProposerSignature">
/// Provenance (R4.1). Rides the generation lineage and is hash-bound into the certificate's
/// generation-depth input, so "which model produced this" is evidence rather than prose.
/// </param>
public sealed record ProposedSource(
    string SourceCode,
    string TypeName,
    string ProposerSignature);

/// <summary>
/// Produces a candidate for an objective. Deliberately core-typed (no ObjectiveDocument):
/// ObjectiveDocument lives above this layer, so the port takes the declarations it needs
/// as primitives — the same shape <c>AutonomousIterationHarness</c> uses.
///
/// <para>Returning null means "no proposal for this objective", which is a normal outcome
/// and must leave the objective untouched. Throwing means the proposer itself failed;
/// neither is ever evidence about a candidate.</para>
/// </summary>
public interface IProposalSource
{
    /// <summary>Proposes a candidate, or null when none is available.</summary>
    Task<ProposedSource?> ProposeAsync(ProposalRequest request, CancellationToken cancellationToken = default);
}
