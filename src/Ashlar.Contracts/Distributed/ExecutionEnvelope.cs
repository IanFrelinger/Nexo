namespace Ashlar.Contracts.Distributed;

/// <summary>
/// Signed-intent wrapper an edge, peer, or cluster uses to request work.
/// Framework contract only: products transport and persist these envelopes;
/// they do not change verification rules.
/// </summary>
/// <param name="EnvelopeId">Caller-generated unique id for this request.</param>
/// <param name="SourceNodeId">Stable identity of the issuing node.</param>
/// <param name="Target">Intended fulfillment location.</param>
/// <param name="WorkloadKind">Product-defined workload label (for example <c>brick.execute</c>).</param>
/// <param name="PayloadHash">Hex or multibase digest of the payload bytes the worker must bind.</param>
/// <param name="PolicyPackId">Policy pack the receiver must evaluate before admission.</param>
/// <param name="IssuedAtUtc">UTC timestamp when the issuer created the envelope.</param>
/// <param name="AllowedCapabilities">Capability names the worker may exercise. Empty means none.</param>
/// <param name="MaxDuration">Optional wall-clock budget. Null means the receiver's default.</param>
/// <param name="Signature">Optional issuer signature over the envelope canonical bytes.</param>
public sealed record ExecutionEnvelope(
    string EnvelopeId,
    string SourceNodeId,
    ExecutionTarget Target,
    string WorkloadKind,
    string PayloadHash,
    string PolicyPackId,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyList<string>? AllowedCapabilities = null,
    TimeSpan? MaxDuration = null,
    byte[]? Signature = null)
{
    /// <summary>
    /// Builds an envelope after rejecting blank required fields, undefined
    /// targets, missing timestamps, non-positive budgets, and malformed digests.
    /// </summary>
    public static ExecutionEnvelope Create(
        string envelopeId,
        string sourceNodeId,
        ExecutionTarget target,
        string workloadKind,
        string payloadHash,
        string policyPackId,
        DateTimeOffset issuedAtUtc,
        IReadOnlyList<string>? allowedCapabilities = null,
        TimeSpan? maxDuration = null,
        byte[]? signature = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envelopeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workloadKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPackId);
        DistributedContractGuard.Defined(target, nameof(target));
        DistributedContractGuard.Timestamp(issuedAtUtc, nameof(issuedAtUtc));
        DistributedContractGuard.Duration(maxDuration, nameof(maxDuration));

        return new ExecutionEnvelope(
            envelopeId.Trim(),
            sourceNodeId.Trim(),
            target,
            workloadKind.Trim(),
            DistributedContractGuard.Digest(payloadHash, nameof(payloadHash)),
            policyPackId.Trim(),
            issuedAtUtc,
            DistributedContractGuard.Capabilities(allowedCapabilities),
            maxDuration,
            signature);
    }
}
