namespace Nexo.Core.Application.Certification.Models;

/// <summary>Outcome of a composition certification gate evaluation.</summary>
public sealed record CompositionCertificationDecision
{
    /// <summary>Whether the composition was admitted into the certified catalog.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Persisted certification record for audit and reuse.</summary>
    public required CompositionCertificationRecord Record { get; init; }

    /// <summary>Name of the certification check that failed, when not admitted.</summary>
    public string? FailureCheck { get; init; }
}
