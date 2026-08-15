namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Result of running the certification gate (ADMIT or REJECT).
/// </summary>
public sealed record CertificationDecision
{
    /// <summary>Whether the brick or composition was admitted.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Signed certification record for the decision.</summary>
    public required CertificationRecord Record { get; init; }

    /// <summary>Identifier of the check that caused rejection, when not admitted.</summary>
    public string? FailureCheck { get; init; }

    /// <summary>
    /// Structured findings from diagnostic probes on rejection (G4). Additive evidence
    /// for the repair loop and the digest; the verbatim gate feedback on the record's
    /// reason is unchanged by probing.
    /// </summary>
    public IReadOnlyList<Ports.DiagnosticProbeFinding> ProbeFindings { get; init; } =
        Array.Empty<Ports.DiagnosticProbeFinding>();
}
