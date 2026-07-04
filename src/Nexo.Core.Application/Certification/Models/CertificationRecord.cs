namespace Nexo.Core.Application.Certification.Models;

/// <summary>
/// Signed certification admission record emitted on ADMIT.
/// </summary>
public sealed record CertificationRecord
{
    /// <summary>Certification status label (e.g. ADMIT, REJECT).</summary>
    public required string Status { get; init; }

    /// <summary>Certification stage that produced this record.</summary>
    public required string Stage { get; init; }

    /// <summary>Whether the brick was admitted for runtime use.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Whether the record carries a cryptographic signature.</summary>
    public required bool Signed { get; init; }

    /// <summary>UTC timestamp when certification completed.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Brick identifier under certification.</summary>
    public required string BrickId { get; init; }

    /// <summary>Hash of the certified source content.</summary>
    public string? ContentHash { get; init; }

    /// <summary>Fraction of mutants that escaped detection (0.0–1.0).</summary>
    public double? EscapeRate { get; init; }

    /// <summary>Total mutants generated during certification.</summary>
    public int? TotalMutants { get; init; }

    /// <summary>Mutants that survived certification checks.</summary>
    public int? SurvivingMutants { get; init; }

    /// <summary>Identifiers of mutants killed by certification.</summary>
    public IReadOnlyList<string> KilledMutants { get; init; } = Array.Empty<string>();

    /// <summary>Identifiers of mutants that survived certification.</summary>
    public IReadOnlyList<string> SurvivingMutantIds { get; init; } = Array.Empty<string>();

    /// <summary>Cryptographic signature of the admission record.</summary>
    public string? Signature { get; init; }

    /// <summary>Human-readable reason for admission or rejection.</summary>
    public string? Reason { get; init; }

    /// <summary>Gate identifier that produced this record.</summary>
    public string? Gate { get; init; }
}
