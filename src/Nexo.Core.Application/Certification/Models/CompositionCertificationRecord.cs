namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Signed composition certification admission record emitted on ADMIT.
/// </summary>
public sealed record CompositionCertificationRecord
{
    public required string Status { get; init; }
    public required string Stage { get; init; }
    public required bool Admitted { get; init; }
    public required bool Signed { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string CompositionId { get; init; }
    public double? CompositionEscapeRate { get; init; }
    public int? TotalStructuralMutants { get; init; }
    public int? SurvivingStructuralMutants { get; init; }
    public IReadOnlyList<string> KilledStructuralMutantIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SurvivingStructuralMutantIds { get; init; } = Array.Empty<string>();
    public string? Signature { get; init; }
    public string? Reason { get; init; }
    public string? Gate { get; init; }
}

public sealed record CompositionCertificationDecision
{
    public required bool Admitted { get; init; }
    public required CompositionCertificationRecord Record { get; init; }
    public string? FailureCheck { get; init; }
}

public sealed record CompositionCertificationRequest
{
    public required CompositionSpec Spec { get; init; }
    public required CompositionWitnessSpec Witness { get; init; }
    public string? WiringMetadata { get; init; }
}
