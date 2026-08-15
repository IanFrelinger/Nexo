namespace Nexo.Certification.Contracts;

/// <summary>
/// One entry in the effort ledger: a proposal or repair attempt and its outcome
/// (trust-loop spec §2.1 <c>attempts</c>). Order is semantic: entries appear in
/// attempt order.
/// </summary>
public sealed record CertificationAttempt
{
    /// <summary>1-based attempt ordinal.</summary>
    public required int Index { get; init; }

    /// <summary>Attempt outcome (e.g. <c>pass</c>, <c>fail</c>).</summary>
    public required string Outcome { get; init; }

    /// <summary>Failure category when the attempt failed.</summary>
    public string? FailureCategory { get; init; }

    /// <summary>Attempt duration in seconds, when measured.</summary>
    public double? DurationSeconds { get; init; }
}
