namespace Nexo.Certification.Contracts;

/// <summary>
/// Portable certification record for external consumers (JSON sidecar).
/// </summary>
public sealed record CertificationRecordData
{
    /// <summary>Overall certification outcome (e.g. <c>PASS</c>, <c>FAIL</c>).</summary>
    public required string Status { get; init; }

    /// <summary>Certification pipeline stage that produced this record.</summary>
    public required string Stage { get; init; }

    /// <summary>Whether the brick was admitted into the certified catalog.</summary>
    public required bool Admitted { get; init; }

    /// <summary>Whether an HMAC signature is present and was verified at write time.</summary>
    public required bool Signed { get; init; }

    /// <summary>UTC timestamp when the certification decision was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Certified brick identifier.</summary>
    public required string BrickId { get; init; }

    /// <summary>SHA-256 hash of canonical brick source bound into this record.</summary>
    public string? ContentHash { get; init; }

    /// <summary>Mutant escape rate observed during certification (0.0–1.0).</summary>
    public double? EscapeRate { get; init; }

    /// <summary>Total mutants generated during certification.</summary>
    public int? TotalMutants { get; init; }

    /// <summary>Mutants that survived the certification test suite.</summary>
    public int? SurvivingMutants { get; init; }

    /// <summary>Identifiers of mutants killed by the test suite.</summary>
    public IReadOnlyList<string> KilledMutants { get; init; } = Array.Empty<string>();

    /// <summary>Identifiers of mutants that survived certification.</summary>
    public IReadOnlyList<string> SurvivingMutantIds { get; init; } = Array.Empty<string>();

    /// <summary>Base64 HMAC-SHA256 signature over the canonical payload.</summary>
    public string? Signature { get; init; }

    /// <summary>Human-readable reason when certification failed or was deferred.</summary>
    public string? Reason { get; init; }

    /// <summary>Gate name that emitted or validated this record.</summary>
    public string? Gate { get; init; }
}
