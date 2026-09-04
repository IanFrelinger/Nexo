namespace Ashlar.Contracts.Distributed;

/// <summary>Terminal status recorded on <see cref="ResultEvidence"/>.</summary>
public enum ResultEvidenceStatus
{
    /// <summary>Worker completed and produced an output digest.</summary>
    Succeeded = 0,

    /// <summary>Worker ran and failed under policy or runtime error.</summary>
    Failed = 1,

    /// <summary>Admission or policy refused to run the envelope.</summary>
    Rejected = 2
}

/// <summary>
/// Signed result the fulfilling node returns to the issuer. A cluster and an
/// edge use the same shape so managed vs self-hosted ownership cannot weaken
/// verification.
/// </summary>
/// <param name="EnvelopeId">Id of the <see cref="ExecutionEnvelope"/> this evidence closes.</param>
/// <param name="TaskId">Scheduler or local-run identifier.</param>
/// <param name="Status">Terminal outcome.</param>
/// <param name="OutputHash">Digest of the result bytes, or empty when rejected before execution.</param>
/// <param name="CompletedAtUtc">UTC timestamp when the worker finalized the result.</param>
/// <param name="CertificationRecordId">Optional certification record bound to the admitted artifact.</param>
/// <param name="Detail">Optional human-readable failure or rejection reason.</param>
/// <param name="Signature">Optional worker or cluster signature over the evidence canonical bytes.</param>
public sealed record ResultEvidence(
    string EnvelopeId,
    string TaskId,
    ResultEvidenceStatus Status,
    string OutputHash,
    DateTimeOffset CompletedAtUtc,
    string? CertificationRecordId = null,
    string? Detail = null,
    byte[]? Signature = null)
{
    /// <summary>
    /// Builds evidence after rejecting blank required fields. Succeeded and
    /// failed evidence require a digest; rejected-before-execution may be empty.
    /// </summary>
    public static ResultEvidence Create(
        string envelopeId,
        string taskId,
        ResultEvidenceStatus status,
        string outputHash,
        DateTimeOffset completedAtUtc,
        string? certificationRecordId = null,
        string? detail = null,
        byte[]? signature = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envelopeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentNullException.ThrowIfNull(outputHash);
        DistributedContractGuard.Defined(status, nameof(status));
        DistributedContractGuard.Timestamp(completedAtUtc, nameof(completedAtUtc));

        var hash = status is ResultEvidenceStatus.Succeeded or ResultEvidenceStatus.Failed
            ? DistributedContractGuard.Digest(outputHash, nameof(outputHash))
            : DistributedContractGuard.OptionalDigest(outputHash, nameof(outputHash));

        return new ResultEvidence(
            envelopeId.Trim(),
            taskId.Trim(),
            status,
            hash,
            completedAtUtc,
            certificationRecordId,
            detail,
            signature);
    }
}
