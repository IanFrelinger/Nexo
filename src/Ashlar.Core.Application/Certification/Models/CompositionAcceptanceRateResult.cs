namespace Ashlar.Core.Application.Certification.Models;

/// <summary>Aggregated acceptance rate across a certification replay batch.</summary>
/// <param name="AcceptanceRate">Fraction of sequences where replay matched recorded admission (0.0–1.0).</param>
/// <param name="Admits">Number of sequences admitted on replay.</param>
/// <param name="Total">Total sequences evaluated.</param>
/// <param name="Entries">Per-sequence comparison details.</param>
public sealed record CompositionAcceptanceRateResult(
    double AcceptanceRate,
    int Admits,
    int Total,
    IReadOnlyList<CompositionAcceptanceRateEntryResult> Entries);
