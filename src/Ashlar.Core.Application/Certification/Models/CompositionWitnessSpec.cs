namespace Ashlar.Core.Application.Certification.Models;

/// <summary>
/// Independent end-to-end witness for a composition (not derived from constituent witnesses).
/// </summary>
/// <param name="CompositionId">Composition identifier under certification.</param>
/// <param name="Cases">End-to-end input→expected-output witness cases.</param>
public sealed record CompositionWitnessSpec(
    string CompositionId,
    IReadOnlyList<WitnessCase> Cases);
