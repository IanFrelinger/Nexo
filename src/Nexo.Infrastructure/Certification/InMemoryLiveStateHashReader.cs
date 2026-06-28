using Nexo.Certification.State;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Fixed live state hash for tests and deterministic hosts.
/// </summary>
public sealed class InMemoryLiveStateHashReader : ILiveStateHashReader
{
    private readonly string? _stateHash;

    public InMemoryLiveStateHashReader(string? stateHash) => _stateHash = stateHash;

    public LiveStateHashReadResult ReadCurrentStateHash() =>
        string.IsNullOrWhiteSpace(_stateHash)
            ? new LiveStateHashReadResult(false, null)
            : new LiveStateHashReadResult(true, _stateHash);
}
