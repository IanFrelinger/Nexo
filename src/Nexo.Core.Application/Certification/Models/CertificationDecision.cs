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
}
