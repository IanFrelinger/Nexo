namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// Revoked certificate content hashes (autonomous self-extension spec R5.3): a rolled-back
/// artifact is quarantined — its certificate is marked revoked and the swap host refuses
/// the hash PERMANENTLY, bit-identical resubmission included. R5.4 chain propagation
/// (certificates whose inputs include a revoked artifact become suspect) builds on this
/// same list.
/// </summary>
public interface ICertificateRevocationList
{
    /// <summary>Whether the given certificate content hash is revoked.</summary>
    bool IsRevoked(string contentHash);

    /// <summary>Revokes a content hash with the reason recorded for the digest.</summary>
    void Revoke(string contentHash, string reason);

    /// <summary>The recorded reason, when revoked; null otherwise.</summary>
    string? TryGetReason(string contentHash);
}
