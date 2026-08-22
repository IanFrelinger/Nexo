namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// Signed composition certification admission record emitted on ADMIT.
/// </summary>
public sealed record CompositionCertificationRecord
{
    /// <summary>Certification status label (e.g. ADMIT, REJECT).</summary>
    public required string Status { get; init; }

    /// <summary>Certification stage that produced this record.</summary>
    public required string Stage { get; init; }

    /// <summary>Whether the composition was admitted for runtime use.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Whether the record carries a cryptographic signature.</summary>
    public required bool Signed { get; init; }

    /// <summary>UTC timestamp when certification completed.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Composition identifier under certification.</summary>
    public required string CompositionId { get; init; }

    /// <summary>Fraction of structural mutants that escaped detection (0.0–1.0).</summary>
    public double? CompositionEscapeRate { get; init; }

    /// <summary>Total structural mutants generated during certification.</summary>
    public int? TotalStructuralMutants { get; init; }

    /// <summary>Structural mutants that survived certification checks.</summary>
    public int? SurvivingStructuralMutants { get; init; }

    /// <summary>Identifiers of structural mutants killed by certification.</summary>
    public IReadOnlyList<string> KilledStructuralMutantIds { get; init; } = Array.Empty<string>();

    /// <summary>Identifiers of structural mutants that survived certification.</summary>
    public IReadOnlyList<string> SurvivingStructuralMutantIds { get; init; } = Array.Empty<string>();

    /// <summary>Cryptographic signature of the admission record.</summary>
    public string? Signature { get; init; }

    /// <summary>Human-readable reason for admission or rejection.</summary>
    public string? Reason { get; init; }

    /// <summary>Gate identifier that produced this record.</summary>
    public string? Gate { get; init; }
}
