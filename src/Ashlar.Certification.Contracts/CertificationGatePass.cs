namespace Ashlar.Certification.Contracts;

/// <summary>
/// One entry in the ordered chain of gates evaluated for a certification record
/// (trust-loop spec §2.1 <c>gates_passed</c>). Order is semantic: entries appear
/// in the order the gates ran.
/// </summary>
public sealed record CertificationGatePass
{
    /// <summary>Stable gate identifier (e.g. <c>mutation-gate</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Gate implementation version active when the gate passed.</summary>
    public string? Version { get; init; }

    /// <summary>Machine-readable gate configuration summary (e.g. <c>escapeRateThreshold=0</c>).</summary>
    public string? Configuration { get; init; }
}
