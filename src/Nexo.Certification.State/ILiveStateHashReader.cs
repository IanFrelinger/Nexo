namespace Nexo.Certification.State;

/// <summary>
/// Tier-3 seam: reads the live mutable store's current schema-bound state hash for comparison
/// against the attested log head (<see cref="AttestedStateLog.CurrentStateHash"/>).
/// Tier-1/Tier-2 verification does not detect divergence between a valid log and live storage.
/// </summary>
public interface ILiveStateHashReader
{
    LiveStateHashReadResult ReadCurrentStateHash();
}

/// <summary>
/// Outcome of reading the live state store hash.
/// </summary>
public sealed class LiveStateHashReadResult
{
    public LiveStateHashReadResult(bool found, string? stateHash)
    {
        Found = found;
        StateHash = stateHash;
    }

    public bool Found { get; }
    public string? StateHash { get; }
}
