using System.Globalization;

namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>
/// What a sandbox session's environment ACTUALLY was, attested by the backend after start
/// (extension spec Part B): resolved image identity, engine version, and the effective
/// resource limits — versus what the spec requested. Certification must record the
/// environment candidates were really evaluated in, and a backend that silently applied
/// weaker limits than requested must be refused, not trusted.
/// </summary>
public sealed record SessionAttestation
{
    /// <summary>Session the attestation describes.</summary>
    public required string SessionId { get; init; }

    /// <summary>Image reference the spec requested (tag, not identity).</summary>
    public required string Image { get; init; }

    /// <summary>Resolved image identity (content digest). Null when the backend could not resolve it.</summary>
    public string? ImageDigest { get; init; }

    /// <summary>Sandbox engine version (e.g. the container server version).</summary>
    public string? EngineVersion { get; init; }

    /// <summary>The limits the spec requested (empty record when none were requested).</summary>
    public required ResourceLimits Requested { get; init; }

    /// <summary>Effective memory cap in bytes; null unknown, 0 = unlimited.</summary>
    public long? EffectiveMemoryBytes { get; init; }

    /// <summary>Effective pids cap; null unknown, 0 or negative = unlimited.</summary>
    public long? EffectivePidsLimit { get; init; }

    /// <summary>Effective CPU cap in nano-cpus; null unknown, 0 = unlimited.</summary>
    public long? EffectiveNanoCpus { get; init; }

    /// <summary>
    /// Network mode the backend actually applied, in the backend's own vocabulary (for a
    /// container engine: <c>none</c>, <c>bridge</c>, …); null when the backend did not
    /// report it. Recorded so the certificate carries the containment that was applied,
    /// not merely the containment that was requested.
    /// </summary>
    public string? EffectiveNetworkMode { get; init; }

    /// <summary>Whether the sandbox's root filesystem was sealed read-only; null unknown.</summary>
    public bool? EffectiveReadOnlyRootFilesystem { get; init; }

    /// <summary>Privileges the backend dropped (backend encoding, e.g. <c>ALL</c>); empty when none or unknown.</summary>
    public IReadOnlyList<string> EffectiveDroppedCapabilities { get; init; } = Array.Empty<string>();

    /// <summary>Security options the backend applied (e.g. <c>no-new-privileges</c>); empty when none or unknown.</summary>
    public IReadOnlyList<string> EffectiveSecurityOptions { get; init; } = Array.Empty<string>();

    /// <summary>UTC time the attestation was taken.</summary>
    public required DateTimeOffset AttestedAt { get; init; }

    /// <summary>
    /// Every way the effective environment is WEAKER than requested. Empty means the
    /// backend honored the request. Fail-closed semantics: a requested limit that cannot
    /// be verified (unparsable request, unknown effective value, or effective=unlimited)
    /// is a shortfall — "probably capped" is not an attestation. Stricter-than-requested
    /// is never a shortfall. Wall-clock timeout is deliberately not checked here: it is
    /// enforced by the session deadline + reaper, not by the engine.
    /// </summary>
    public IReadOnlyList<string> FindShortfalls()
    {
        var shortfalls = new List<string>();

        if (!string.IsNullOrWhiteSpace(Requested.Memory))
        {
            var requestedBytes = ParseMemoryToBytes(Requested.Memory!);
            if (requestedBytes is null)
                shortfalls.Add($"requested memory '{Requested.Memory}' is unverifiable (unparsable request)");
            else if (EffectiveMemoryBytes is null or <= 0)
                shortfalls.Add($"requested memory {Requested.Memory} but the effective cap is unlimited/unknown");
            else if (EffectiveMemoryBytes.Value > requestedBytes.Value)
                shortfalls.Add($"requested memory {Requested.Memory} but the effective cap is weaker ({EffectiveMemoryBytes} bytes)");
        }

        if (Requested.Pids is > 0)
        {
            if (EffectivePidsLimit is null or <= 0)
                shortfalls.Add($"requested pids limit {Requested.Pids} but the effective cap is unlimited/unknown");
            else if (EffectivePidsLimit.Value > Requested.Pids.Value)
                shortfalls.Add($"requested pids limit {Requested.Pids} but the effective cap is weaker ({EffectivePidsLimit})");
        }

        if (!string.IsNullOrWhiteSpace(Requested.Cpus))
        {
            var requestedNano = ParseCpusToNano(Requested.Cpus!);
            if (requestedNano is null)
                shortfalls.Add($"requested cpus '{Requested.Cpus}' is unverifiable (unparsable request)");
            else if (EffectiveNanoCpus is null or <= 0)
                shortfalls.Add($"requested cpus {Requested.Cpus} but the effective cap is unlimited/unknown");
            else if (EffectiveNanoCpus.Value > requestedNano.Value)
                shortfalls.Add($"requested cpus {Requested.Cpus} but the effective cap is weaker ({EffectiveNanoCpus} nano-cpus)");
        }

        return shortfalls;
    }

    /// <summary>
    /// Refuses an environment weaker than requested (spec Part B "refuse below floor"):
    /// throws with every shortfall named. Call after session start, before any candidate
    /// work runs in the session.
    /// </summary>
    public void ThrowIfBelowRequested()
    {
        var shortfalls = FindShortfalls();
        if (shortfalls.Count > 0)
        {
            throw new InvalidOperationException(
                $"Sandbox session '{SessionId}' environment is weaker than requested; refusing to use it: "
                + string.Join(" | ", shortfalls));
        }
    }

    /// <summary>Parses backend memory encodings (<c>2g</c>, <c>512m</c>, <c>1024</c>) to bytes.</summary>
    public static long? ParseMemoryToBytes(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
            return null;

        var multiplier = 1L;
        var last = trimmed[trimmed.Length - 1];
        if (last is 'b' or 'k' or 'm' or 'g')
        {
            multiplier = last switch
            {
                'k' => 1024L,
                'm' => 1024L * 1024,
                'g' => 1024L * 1024 * 1024,
                _ => 1L,
            };
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
        }

        return long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number * multiplier
            : null;
    }

    /// <summary>Parses backend cpu encodings (<c>2</c>, <c>0.5</c>) to nano-cpus.</summary>
    public static long? ParseCpusToNano(string value) =>
        decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var cpus) && cpus > 0
            ? (long)(cpus * 1_000_000_000m)
            : null;
}
