namespace Nexo.Certification.State;

/// <summary>
/// Immutable certified state transition with tamper-evident hash-chain linkage.
/// </summary>
public sealed record CertifiedTransition
{
    /// <summary>Hash of the certified state before this transition.</summary>
    public string PriorStateHash { get; init; } = string.Empty;

    /// <summary>Action label that caused the transition.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Content hash of the behavior certificate involved in the transition.</summary>
    public string BehaviorCertContentHash { get; init; } = string.Empty;

    /// <summary>Hash of the certified state after this transition.</summary>
    public string ResultingStateHash { get; init; } = string.Empty;

    /// <summary>Hash of the previous entry in the tamper-evident chain.</summary>
    public string PrevEntryHash { get; init; } = string.Empty;

    /// <summary>Hash of this transition entry in the tamper-evident chain.</summary>
    public string EntryHash { get; init; } = string.Empty;

    /// <summary>
    /// Genesis sentinel for the first log entry's <see cref="PrevEntryHash"/>.
    /// </summary>
    public const string GenesisPrevEntryHash = "";
}
