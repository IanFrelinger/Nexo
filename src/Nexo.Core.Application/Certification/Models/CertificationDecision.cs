namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Result of running the certification gate (ADMIT or REJECT).
/// </summary>
public sealed record CertificationDecision
{
    public required bool Admitted { get; init; }
    public required CertificationRecord Record { get; init; }
    public string? FailureCheck { get; init; }
}
