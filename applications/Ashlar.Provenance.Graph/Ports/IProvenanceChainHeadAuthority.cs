namespace Ashlar.Provenance.Graph.Ports;

/// <summary>
/// Supplies the current chain head from authoritative certificate artifacts,
/// independently of graph metadata.
/// </summary>
public interface IProvenanceChainHeadAuthority
{
    Task<string> GetCurrentChainHeadHashAsync(CancellationToken cancellationToken = default);
}

/// <summary>Fail-closed default used until an authoritative source is configured.</summary>
public sealed class UnavailableProvenanceChainHeadAuthority : IProvenanceChainHeadAuthority
{
    public Task<string> GetCurrentChainHeadHashAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("No authoritative provenance chain-head source is configured.");
}

/// <summary>Mutable authority intended for deterministic tests and controlled hosts.</summary>
public sealed class InMemoryProvenanceChainHeadAuthority : IProvenanceChainHeadAuthority
{
    private string? _chainHeadHash;

    public InMemoryProvenanceChainHeadAuthority()
    {
    }

    public InMemoryProvenanceChainHeadAuthority(string chainHeadHash) =>
        SetCurrentChainHead(chainHeadHash);

    public void SetCurrentChainHead(string chainHeadHash)
    {
        if (string.IsNullOrWhiteSpace(chainHeadHash))
            throw new ArgumentException("Chain-head hash is required.", nameof(chainHeadHash));
        _chainHeadHash = chainHeadHash;
    }

    public Task<string> GetCurrentChainHeadHashAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_chainHeadHash
            ?? throw new InvalidOperationException("Authoritative chain head has not been initialized."));
}
