namespace Nexo.Core.Application.Certification.Models;

/// <summary>Why one witness case failed, as structured data rather than prose.</summary>
public enum WitnessFindingKind
{
    /// <summary>The candidate threw (or could not be loaded) on this case.</summary>
    Threw,

    /// <summary>The candidate produced no value for an expected key.</summary>
    MissingKey,

    /// <summary>The candidate produced a value that does not match the expectation.</summary>
    Mismatch,

    /// <summary>The execution backend returned no observation for this case.</summary>
    NoObservation,
}

/// <summary>
/// One witness failure, structured. Carried on the certification decision so that repair
/// feedback can be PROJECTED at a chosen disclosure level instead of re-parsed from the
/// human-facing reason string. The two views serve different audiences: the human digest
/// keeps everything; the proposer channel is derived from this by policy.
/// </summary>
/// <param name="CaseIndex">Witness case index.</param>
/// <param name="Kind">What went wrong.</param>
/// <param name="Key">Output key, when the finding concerns one.</param>
/// <param name="Expected">Expected value — the witness's secret. Only ever shown at full disclosure.</param>
/// <param name="Actual">The candidate's own observed value — always safe to show back to it.</param>
/// <param name="Detail">Free-text detail (exception message, etc.).</param>
public sealed record WitnessFinding(
    int CaseIndex,
    WitnessFindingKind Kind,
    string? Key = null,
    string? Expected = null,
    string? Actual = null,
    string? Detail = null);
