namespace Ashlar.Certification.State;

/// <summary>
/// Append-only ordered certified transitions. Current state is the last entry's
/// <see cref="CertifiedTransition.ResultingStateHash"/>; live values live elsewhere.
/// </summary>
public sealed class AttestedStateLog
{
    public AttestedStateLog(IReadOnlyList<CertifiedTransition> transitions)
    {
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
    }

    public IReadOnlyList<CertifiedTransition> Transitions { get; }

    public int Count => Transitions.Count;

    public string? CurrentStateHash =>
        Transitions.Count > 0 ? Transitions[Transitions.Count - 1].ResultingStateHash : null;
}
