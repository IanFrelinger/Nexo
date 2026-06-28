namespace Nexo.Certification.State;

/// <summary>
/// Fixed live state hash for tests, bundled sidecars, and deterministic hosts.
/// </summary>
public sealed class FixedLiveStateHashReader : ILiveStateHashReader
{
    private readonly string? _stateHash;

    public FixedLiveStateHashReader(string? stateHash) => _stateHash = stateHash;

    public LiveStateHashReadResult ReadCurrentStateHash() =>
        string.IsNullOrWhiteSpace(_stateHash)
            ? new LiveStateHashReadResult(false, null)
            : new LiveStateHashReadResult(true, _stateHash);
}
