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
    /// Validates required fields on every construction path, including
    /// <c>new</c>, <c>with</c>, and JSON deserialize.
    /// </summary>
    public ExecutionEnvelope
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvelopeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(SourceNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(WorkloadKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(PayloadHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyPackId);
        DistributedContractGuard.Defined(Target, nameof(Target));
        DistributedContractGuard.Timestamp(IssuedAtUtc, nameof(IssuedAtUtc));
        DistributedContractGuard.Duration(MaxDuration, nameof(MaxDuration));

        EnvelopeId = EnvelopeId.Trim();
        SourceNodeId = SourceNodeId.Trim();
        WorkloadKind = WorkloadKind.Trim();
        PayloadHash = DistributedContractGuard.Digest(PayloadHash, nameof(PayloadHash));
        PolicyPackId = PolicyPackId.Trim();
        AllowedCapabilities = DistributedContractGuard.Capabilities(AllowedCapabilities);
    }

    /// <summary>
    /// Builds an envelope after rejecting blank required fields.
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
        byte[]? signature = null) =>
        new(
            envelopeId,
            sourceNodeId,
            target,
            workloadKind,
            payloadHash,
            policyPackId,
            issuedAtUtc,
            allowedCapabilities,
            maxDuration,
            signature);
}
