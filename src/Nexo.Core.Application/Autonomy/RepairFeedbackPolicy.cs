using System.Diagnostics.CodeAnalysis;

namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// How much of a rejection a proposer may see when it is asked to repair its candidate.
/// The levels are ordered by disclosure; each strictly contains the one before it.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public enum RepairDisclosure
{
    /// <summary>
    /// Which check failed, nothing more. The proposer learns "correctness" or "mutation" and
    /// must reason from the objective and contract alone. Preserves generation-blindness
    /// completely; expect slow convergence.
    /// </summary>
    CheckOnly = 0,

    /// <summary>
    /// The default. The failing check, the witness case index, the output key, and the
    /// proposer's OWN observed value — never the expected value. This is the largest
    /// disclosure that keeps the certificate non-vacuous: everything shown is either
    /// already the proposer's (its own output) or a location, not an answer. The evidence
    /// tonight is that a 7B model repaired a null-vs-empty defect from exactly this: it
    /// needed "case 0, failureCode, you produced &lt;null&gt;", not the value "".
    /// </summary>
    OwnOutput = 1,

    /// <summary>
    /// Everything, including expected witness values. This LEAKS the witness to the
    /// proposer: a candidate that converges only under this level converged on the tests,
    /// not the contract, and its ADMIT is a weaker claim. Deliberately opt-in; intended
    /// for small local models that cannot converge otherwise, and for use in hold mode
    /// where a human still reads the evidence before anything swaps.
    /// </summary>
    Full = 2,
}

/// <summary>
/// The operator's dial for the repair channel — deliberately independent of any model.
/// A large hosted model and a 7B local one need different settings on the same loop, so
/// nothing here is compiled in.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class RepairFeedbackPolicy
{
    /// <summary>What a proposer may see. Default <see cref="RepairDisclosure.OwnOutput"/>.</summary>
    public RepairDisclosure Disclosure { get; set; } = RepairDisclosure.OwnOutput;

    /// <summary>
    /// Repair rounds allowed per objective before it is held for a human. Bounds what a
    /// proposer can learn about the witness by iterating (even redacted feedback lets a
    /// proposer probe case indices, given enough rounds), and keeps acceptance rate an
    /// honest measurement rather than a number the loop can inflate by trying forever.
    /// 0 disables repair entirely: every rejection is terminal. Measured (S3 in the evidence
    /// ledger): a 7B local model converged on 3/5 objectives within the default of 2; small
    /// models may want 3, large hosted ones rarely need more than 1.
    /// </summary>
    public int MaxAttemptsPerObjective { get; set; } = 2;

    /// <summary>
    /// Findings included per feedback message, in deterministic (case, key) order, before
    /// truncation. Mirrors the analyzer fence's own cap. Smaller contexts want fewer.
    /// </summary>
    public int MaxFindings { get; set; } = 8;

    /// <summary>
    /// Whether mutation-leg feedback names surviving mutant ids (which encode a source line).
    /// A line is a location, not an answer, so this is safe at every disclosure level; it is
    /// a switch only because some proposers do worse, not better, when told where to look.
    /// </summary>
    public bool IncludeMutantLocations { get; set; } = true;

    /// <summary>A policy that shares nothing but the failing check.</summary>
    public static RepairFeedbackPolicy Blind() => new() { Disclosure = RepairDisclosure.CheckOnly };

    /// <summary>The default: locations and the proposer's own output, never the witness.</summary>
    public static RepairFeedbackPolicy Default() => new();

    /// <summary>Full disclosure. Read the <see cref="RepairDisclosure.Full"/> caveat first.</summary>
    public static RepairFeedbackPolicy FullDisclosure() => new() { Disclosure = RepairDisclosure.Full };
}
