namespace Nexo.Certification.State;

/// <summary>
/// Immutable certified state transition with tamper-evident hash-chain linkage.
/// </summary>
public sealed record CertifiedTransition
{
    public string PriorStateHash { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string BehaviorCertContentHash { get; init; } = string.Empty;
    public string ResultingStateHash { get; init; } = string.Empty;
    public string PrevEntryHash { get; init; } = string.Empty;
    public string EntryHash { get; init; } = string.Empty;

    /// <summary>
    /// Genesis sentinel for the first log entry's <see cref="PrevEntryHash"/>.
    /// </summary>
    public const string GenesisPrevEntryHash = "";
}
