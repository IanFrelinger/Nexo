using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Nexo.Core.Application.Autonomy;

namespace Nexo.Infrastructure.Certification.HotSwap;

/// <summary>
/// Process-local revocation list. Revocation only accumulates — there is deliberately no
/// unrevoke operation on the interface or here: readmitting a quarantined artifact means
/// re-certifying a NEW candidate (whose fresh gates produce a fresh hash), never
/// resurrecting the old hash (R5.3).
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed class InMemoryCertificateRevocationList : ICertificateRevocationList
{
    private readonly ConcurrentDictionary<string, string> _revoked = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsRevoked(string contentHash) =>
        !string.IsNullOrWhiteSpace(contentHash) && _revoked.ContainsKey(contentHash);

    /// <inheritdoc />
    public void Revoke(string contentHash, string reason)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash is required.", nameof(contentHash));
        _revoked.TryAdd(contentHash, reason ?? "");
    }

    /// <inheritdoc />
    public string? TryGetReason(string contentHash) =>
        _revoked.TryGetValue(contentHash ?? "", out var reason) ? reason : null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Snapshot() => _revoked.Keys.ToArray();
}
